using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SharpHook;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Exceptions;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly TaskPoolGlobalHook hook;
        private readonly HashSet<IMixerService> mixerServicesAll = new HashSet<IMixerService>();
        private readonly HashSet<IMixerService> mixerServicesForeground = new HashSet<IMixerService>();
        private readonly Options options;
        private readonly IStorageService<Set> settingsService;

        private FadedControlProvider backgroundFader;
        private ContinuousMixingProvider backgroundMixer;
        private bool isDisposed;
        private bool isLowered;
        private WaveOut output;
        private ContinuousMixingProvider outputMixer;
        private Output outputOptions;
        private IVolumeService volumeService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService, TaskPoolGlobalHook hook)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.hook = hook;

            this.options = settingsService.Contents.Options;

            hook.KeyPressed += OnKeyPressed;
            hook.KeyReleased += OnKeyReleased;

            eventAggregator.GetEvent<MixerStartedEvent>().Subscribe(
                action: p => OnMixerStarted(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixerServicesAll.Contains(p));

            eventAggregator.GetEvent<MixerStoppedEvent>().Subscribe(
                action: p => OnMixerStopped(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixerServicesAll.Contains(p));

            eventAggregator.GetEvent<MixerEndedEvent>().Subscribe(
                action: p => OnMixerEnded(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixerServicesAll.Contains(p));
        }

        #endregion Public Constructors

        #region Public Properties

        public string DeviceName => outputOptions?.DeviceName;

        public WaveFormat Format { get; private set; }

        public bool IsPlaying { get; private set; }

        public OutputType Type { get; private set; }

        public float Volume
        {
            get
            {
                return outputOptions.Volume;
            }
            set
            {
                if (value < Constants.VolumeMin || value > Constants.VolumeMax)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.Volume)
                {
                    outputOptions.Volume = value;

                    if (output != default)
                    {
                        output.Volume = outputOptions.Volume;
                    }
                }
            }
        }

        public double VolumePortionApps
        {
            get
            {
                return outputOptions.VolumePortionApps;
            }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.VolumePortionApps)
                {
                    outputOptions.VolumePortionApps = value;

                    if (isLowered)
                    {
                        Lower();
                    }
                }
            }
        }

        public double VolumePortionBackground
        {
            get
            {
                return outputOptions.VolumePortionBackground;
            }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.VolumePortionBackground)
                {
                    outputOptions.VolumePortionBackground = value;

                    if (isLowered)
                    {
                        Lower();
                    }
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Add(IMixerService mixerService, bool onForeground)
        {
            if (onForeground)
            {
                mixerServicesForeground.Add(mixerService);

                outputMixer.Add(mixerService.Output);
            }
            else
            {
                backgroundMixer.Add(mixerService.Output);
            }

            mixerServicesAll.Add(mixerService);
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public Dictionary<int, string> GetDevices()
        {
            var capabilities = GetCapabilities();

            var result = capabilities.ToDictionary(
                keySelector: c => c.Key,
                elementSelector: c => c.Value.ProductName);

            return result;
        }

        public void Initialize(OutputType type)
        {
            Type = type;

            outputOptions = GetOptions(type);

            volumeService = containerProvider.Resolve<IVolumeService>(
                name: type.ToString());

            Set(
                deviceName: outputOptions.DeviceName,
                takeDefaultIfMissing: true);
        }

        public void Lower()
        {
            if (!isLowered)
            {
                isLowered = true;

                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                var volumeBackground = output.Volume * (float)outputOptions.VolumePortionBackground;

                backgroundFader.Fade(
                    volumeTarget: volumeBackground,
                    milliSeconds: fadeMilliseconds);

                var volumeApps = output.Volume * (float)outputOptions.VolumePortionApps;

                volumeService.Reduce(
                    volumeTarget: volumeApps,
                    milliSeconds: fadeMilliseconds);
            }
        }

        public void Pause()
        {
            mixerServicesAll.OnEach(p => p.Pause());

            PauseVolume();
        }

        public void Raise()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

            isLowered = false;

            backgroundFader.Fade(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: fadeMilliseconds);

            volumeService.Raise(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: fadeMilliseconds);
        }

        public void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false)
        {
            var capabilities = GetCapabilities();

            var deviceNumber = GetDeviceNumber(
                capabilities: capabilities,
                deviceName: deviceName,
                takeDefaultIfMissing: takeDefaultIfMissing);

            CloseOutput();

            outputOptions.DeviceName = capabilities[deviceNumber].ProductName;

            Format = WaveFormat.CreateIeeeFloatWaveFormat(
                sampleRate: sampleRate,
                channels: capabilities[deviceNumber].Channels);

            backgroundMixer = new ContinuousMixingProvider(Format);

            backgroundFader = new FadedControlProvider(
                sourceProvider: backgroundMixer,
                volume: Constants.VolumeMax);

            outputMixer = new ContinuousMixingProvider(Format);

            outputMixer.Add(backgroundFader);

            var waveProvider = new NAudio.Wave.SampleProviders.SampleToWaveProvider(outputMixer);

            output = new WaveOut
            {
                DeviceNumber = deviceNumber,
                Volume = outputOptions.Volume,
            };

            output.Init(
                waveProvider: waveProvider);

            output.Play();

            volumeService.Set(
                deviceName: deviceName);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    CloseOutput();

                    hook.KeyPressed += OnKeyPressed;
                    hook.KeyReleased += OnKeyReleased;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CloseOutput()
        {
            if (output != default)
            {
                output.Stop();

                outputMixer.Dispose();
                backgroundMixer.Dispose();

                output.Dispose();

                volumeService.Deactivate();
                volumeService.Dispose();

                mixerServicesForeground.Clear();
                mixerServicesAll.Clear();
            }
        }

        private Dictionary<int, WaveOutCapabilities> GetCapabilities()
        {
            var result = new Dictionary<int, WaveOutCapabilities>();

            for (var index = -1; index < WaveOut.DeviceCount; index++)
            {
                var capabilities = WaveOut.GetCapabilities(index);

                if (capabilities.Channels == 1 || capabilities.Channels == 2)
                {
                    result.Add(
                        key: index,
                        value: capabilities);
                }
            }

            return result;
        }

        private int GetDeviceNumber(Dictionary<int, WaveOutCapabilities> capabilities, string deviceName,
            bool takeDefaultIfMissing)
        {
            var result = default(int?);

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                result = capabilities
                    .FirstOrDefault(c => c.Value.ProductName == deviceName).Key;
            }

            if (!result.HasValue)
            {
                if (takeDefaultIfMissing)
                {
                    result = capabilities
                        .FirstOrDefault().Key;
                }
                else
                {
                    throw new NoDeviceAvailableException(
                        message: $"There is no device {deviceName} available.");
                }
            }

            if (!result.HasValue)
            {
                throw new NoDeviceAvailableException();
            }

            return result.Value;
        }

        private Output GetOptions(OutputType type)
        {
            if (!settingsService.Contents.Outputs.Any(o => o.Type == type))
            {
                var outputOptions = new Output
                {
                    Type = type,
                    Volume = Constants.VolumeMax,
                };

                settingsService.Contents.Outputs.Add(outputOptions);
            }

            var result = settingsService.Contents.Outputs
                .Single(s => s.Type == type);

            return result;
        }

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (options.KeyPauseOutput.HasValue
                && e?.Data.KeyCode == options.KeyPauseOutput)
            {
                Pause();
            }
            else if (options.KeyLowerVolume.HasValue
                && e?.Data.KeyCode == options.KeyLowerVolume)
            {
                Lower();
            }
        }

        private void OnKeyReleased(object sender, KeyboardHookEventArgs e)
        {
            if (options.KeyLowerVolume.HasValue
                && e?.Data.KeyCode == options.KeyLowerVolume)
            {
                Raise();
            }
        }

        private void OnMixerEnded(IMixerService mixerService)
        {
            if (mixerServicesForeground.Contains(mixerService))
            {
                mixerServicesForeground.Remove(mixerService);
            }

            mixerServicesAll.Remove(mixerService);
        }

        private void OnMixerStarted(IMixerService mixerService)
        {
            if (!IsPlaying)
            {
                IsPlaying = true;

                eventAggregator.GetEvent<OutputStartedEvent>().Publish(this);
            }

            if (mixerServicesForeground.Contains(mixerService))
            {
                Lower();
            }
            else
            {
                PauseVolume();
            }
        }

        private void OnMixerStopped(IMixerService mixerService)
        {
            if (mixerServicesForeground.Contains(mixerService))
            {
                Raise();
            }

            if (!mixerServicesAll.Any(p => p.IsPlaying))
            {
                IsPlaying = false;

                eventAggregator.GetEvent<OutputStoppedEvent>().Publish(this);
            }
        }

        private void PauseVolume()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

            volumeService.Reduce(
                volumeTarget: Constants.VolumeMin,
                milliSeconds: fadeMilliseconds);
        }

        #endregion Private Methods
    }
}