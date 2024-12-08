using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DryIoc.ImTools;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly HashSet<IMixerService> mixersAll = [];
        private readonly HashSet<IMixerService> mixersForeground = [];
        private readonly IStorageService<Set> settingsService;

        private bool isDisposed;
        private bool isLowered;
        private Output outputOptions;
        private ContinuousMixingProvider providerBackground;
        private DynamicFadingProvider providerFader;
        private ContinuousMixingProvider providerForeground;
        private WaveOutEvent providerOutput;
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
                action: SetOutput,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<MixerStartedEvent>().Subscribe(
                action: OnMixerStarted,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: mixersAll.Contains);

            eventAggregator.GetEvent<MixerStoppedEvent>().Subscribe(
                action: OnMixerStopped,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: mixersAll.Contains);

            eventAggregator.GetEvent<MixerEndedEvent>().Subscribe(
                action: OnMixerEnded,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: mixersAll.Contains);
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
                    throw new ArgumentOutOfRangeException(nameof(Volume));
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

                var volumeExternals = providerOutput.Volume * outputOptions.VolumePercentageExternals / (float)Constants.PercentageMax;

                volumeService.Reduce(
                    volumeTarget: volumeExternals,
                    milliSeconds: fadeMilliseconds);
            }
        }

        public void Pause()
        {
            mixersAll.OnEach(p => p.Pause());

            MuteExternals();
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
                    outputOptions.DeviceName = capabilities[deviceNumber.Value].ProductName;
                    outputOptions.SampleRate = sampleRate;

                    Format = WaveFormat.CreateIeeeFloatWaveFormat(
                        sampleRate: sampleRate,
                        channels: capabilities[deviceNumber.Value].Channels);

                    InitializeOutput(
                        deviceNumber: deviceNumber.Value);

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
                    mixersAll.OnEach(p => p.Stop());

                    mixersForeground.Clear();
                    mixersAll.Clear();

                    cancellationTokenSource?.Cancel();

                    providerOutput?.Stop();

                    providerOutput?.Dispose();
                    providerForeground?.Dispose();
                    providerFader?.Dispose();
                    providerBackground?.Dispose();

                    volumeService?.Deactivate();
                    volumeService?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static Dictionary<int, WaveOutCapabilities> GetCapabilities()
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

        private static int GetDeviceNumber(Dictionary<int, WaveOutCapabilities> capabilities, string deviceName,
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

        private void CloseOutput()
        {
            mixersAll.OnEach(p => p.Stop());
            providerOutput?.Stop();

            providerOutput?.Dispose();
            providerForeground?.Dispose();
            providerFader?.Dispose();
            providerBackground?.Dispose();
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

        private void InitializeOutput(int deviceNumber)
        {
            CloseOutput();

            providerBackground = new ContinuousMixingProvider(Format);

            providerFader = new DynamicFadingProvider(
                sourceProvider: providerBackground,
                volume: Constants.VolumeMax);

            providerForeground = new ContinuousMixingProvider(Format);
            providerForeground.Add(providerFader);

            var providerWave = new SampleToWaveProvider(providerForeground);

            providerOutput = new WaveOutEvent
            {
                DeviceNumber = deviceNumber,
                Volume = outputOptions.Volume,
            };

            providerOutput.PlaybackStopped += OnPlaybackStopped;

            providerOutput.Init(
                waveProvider: providerWave);

            Task.Run(
                action: providerOutput.Play,
                cancellationToken: cancellationTokenSource.Token);

            mixersAll.OnEach(m => Add(
                mixerService: m,
                onForeground: mixersForeground.Contains(m)));
        }

        private void MuteExternals()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds;

            volumeService.Mute(
                milliSeconds: fadeMilliseconds);
        }

        private void OnMixerEnded(IMixerService mixerService)
        {
            mixersForeground.Remove(mixerService);

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
                MuteExternals();
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

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != default)
            {
                // TODO: Log exception

                CloseOutput();

                outputOptions.DeviceName = default;

                eventAggregator.GetEvent<OutputUpdatedEvent>().Publish(this);
            }
        }

        private void SetOutput()
        {
            outputOptions = GetOptions(Type);

            volumeService = containerProvider.Resolve<IVolumeService>(
                name: Type.ToString());

            var takeDefaultIfMissing = Type == OutputType.Live;

            Set(
                deviceName: outputOptions.DeviceName,
                takeDefaultIfMissing: takeDefaultIfMissing);
        }

        #endregion Private Methods
    }
}