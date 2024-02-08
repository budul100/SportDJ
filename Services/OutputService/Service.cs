using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Exceptions;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly HashSet<IMixerService> mixersAll = new HashSet<IMixerService>();
        private readonly HashSet<IMixerService> mixersBackground = new HashSet<IMixerService>();
        private readonly HashSet<IMixerService> mixersForeground = new HashSet<IMixerService>();
        private readonly IStorageService<Set> settingsService;

        private CancellationTokenSource cancellationTokenSource;
        private bool isDisposed;
        private bool isLowered;
        private Output outputOptions;
        private ContinuousMixingProvider providerBackground;
        private FadedControlProvider providerFader;
        private ContinuousMixingProvider providerForeground;
        private WaveOut providerOutput;
        private IVolumeService volumeService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: () => InitializeOutput(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<MixerStartedEvent>().Subscribe(
                action: p => OnMixerStarted(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixersAll.Contains(p));

            eventAggregator.GetEvent<MixerStoppedEvent>().Subscribe(
                action: p => OnMixerStopped(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixersAll.Contains(p));

            eventAggregator.GetEvent<MixerEndedEvent>().Subscribe(
                action: p => OnMixerEnded(p),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => mixersAll.Contains(p));
        }

        #endregion Public Constructors

        #region Public Properties

        public string DeviceName => outputOptions?.DeviceName;

        public WaveFormat Format { get; private set; }

        public bool IsActive => providerOutput != default;

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

                    if (providerOutput != default)
                    {
                        providerOutput.Volume = outputOptions.Volume;
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
                mixersForeground.Add(mixerService);

                providerForeground.Add(mixerService.Output);
            }
            else
            {
                mixersBackground.Add(mixerService);

                providerBackground.Add(mixerService.Output);
            }

            mixersAll.Add(mixerService);
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
        }

        public void Lower()
        {
            if (!isLowered)
            {
                isLowered = true;

                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                var volumeBackground = providerOutput.Volume * outputOptions.VolumePercentageBackground / (float)Constants.PercentageMax;

                providerFader.Fade(
                    volumeTarget: volumeBackground,
                    milliSeconds: fadeMilliseconds);

                var volumeApps = providerOutput.Volume * outputOptions.VolumePercentageApps / (float)Constants.PercentageMax;

                volumeService.Reduce(
                    volumeTarget: volumeApps,
                    milliSeconds: fadeMilliseconds);
            }
        }

        public void Pause()
        {
            mixersAll.OnEach(p => p.Pause());

            PauseVolume();
        }

        public void Raise()
        {
            if (isLowered)
            {
                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                isLowered = false;

                providerFader.Fade(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeMilliseconds);

                volumeService.Raise(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeMilliseconds);
            }
        }

        public void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false)
        {
            if (providerOutput == default
                || outputOptions.DeviceName != deviceName
                || outputOptions.SampleRate != sampleRate)
            {
                var capabilities = GetCapabilities();

                var deviceNumber = default(int?);

                try
                {
                    deviceNumber = GetDeviceNumber(
                        capabilities: capabilities,
                        deviceName: deviceName,
                        takeDefaultIfMissing: takeDefaultIfMissing);
                }
                catch (NoDeviceAvailableException)
                { }

                if (deviceNumber.HasValue)
                {
                    CloseOutput();

                    outputOptions.DeviceName = capabilities[deviceNumber.Value].ProductName;
                    outputOptions.SampleRate = sampleRate;

                    Format = WaveFormat.CreateIeeeFloatWaveFormat(
                        sampleRate: sampleRate,
                        channels: capabilities[deviceNumber.Value].Channels);

                    UpdateBackground();
                    UpdateForeground();
                    UpdateOutput(deviceNumber.Value);

                    cancellationTokenSource = new CancellationTokenSource();

                    Task.Run(
                        action: () => providerOutput.Play(),
                        cancellationToken: cancellationTokenSource.Token);

                    volumeService.Set(
                        deviceName: outputOptions.DeviceName);
                }

                eventAggregator.GetEvent<OutputUpdatedEvent>().Publish(this);
            }
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

                    volumeService?.Deactivate();
                    volumeService?.Dispose();

                    mixersForeground.Clear();
                    mixersAll.Clear();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CloseOutput()
        {
            if (providerOutput != default)
            {
                cancellationTokenSource?.Cancel();

                providerOutput.Stop();

                providerForeground.Dispose();
                providerBackground.Dispose();
                providerOutput.Dispose();
            }
        }

        private Dictionary<int, WaveOutCapabilities> GetCapabilities()
        {
            var result = new Dictionary<int, WaveOutCapabilities>();

            for (var index = Constants.IndexAudioDeviceMin; index < WaveOut.DeviceCount; index++)
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

            if (!string.IsNullOrWhiteSpace(deviceName))
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
            if (settingsService?.Contents?.Outputs.Any(o => o.Type == type) != true)
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

        private void InitializeOutput()
        {
            outputOptions = GetOptions(Type);

            volumeService = containerProvider.Resolve<IVolumeService>(
                name: Type.ToString());

            var takeDefaultIfMissing = Type == OutputType.Live;

            Set(
                deviceName: outputOptions.DeviceName,
                takeDefaultIfMissing: takeDefaultIfMissing);
        }

        private void OnMixerEnded(IMixerService mixerService)
        {
            if (mixersForeground.Contains(mixerService))
            {
                mixersForeground.Remove(mixerService);
            }

            mixersAll.Remove(mixerService);
        }

        private void OnMixerStarted(IMixerService mixerService)
        {
            if (!IsPlaying)
            {
                IsPlaying = true;

                eventAggregator.GetEvent<OutputStartedEvent>().Publish(this);
            }

            if (mixersForeground.Contains(mixerService))
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
            if (mixersForeground.Contains(mixerService))
            {
                Raise();
            }

            if (!mixersAll.Any(p => p.IsPlaying))
            {
                IsPlaying = false;

                eventAggregator.GetEvent<OutputStoppedEvent>().Publish(this);
            }
        }

        private void PauseVolume()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnPauseMilliseconds;

            volumeService.Mute(
                milliSeconds: fadeMilliseconds);
        }

        private void UpdateBackground()
        {
            providerBackground = new ContinuousMixingProvider(Format);

            foreach (var mixerBackground in mixersBackground)
            {
                providerBackground.Add(mixerBackground.Output);
            }
        }

        private void UpdateForeground()
        {
            providerForeground = new ContinuousMixingProvider(Format);

            providerFader = new FadedControlProvider(
                sourceProvider: providerBackground,
                volume: Constants.VolumeMax);

            providerForeground.Add(providerFader);

            foreach (var mixerForeground in mixersForeground)
            {
                providerForeground.Add(mixerForeground.Output);
            }
        }

        private void UpdateOutput(int deviceNumber)
        {
            providerOutput = new WaveOut
            {
                DeviceNumber = deviceNumber,
                Volume = outputOptions.Volume,
            };

            var waveProvider = new NAudio.Wave.SampleProviders
                .SampleToWaveProvider(providerForeground);

            providerOutput.Init(
                waveProvider: waveProvider);
        }

        #endregion Private Methods
    }
}