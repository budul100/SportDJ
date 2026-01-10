using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.OutputService.Windows
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IMidiService midiService;
        private readonly HashSet<IMixerService> mixersAll = [];
        private readonly HashSet<IMixerService> mixersForeground = [];
        private readonly Output outputHandler = new();
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private ContinuousMixingProvider backgroundProvider;
        private DynamicFadingProvider fadingProvider;
        private ContinuousMixingProvider foregroundProvider;
        private bool isDisposed;
        private Commons.Models.Settings.Output outputOptions;
        private IVolumeService volumeService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService, IStorageService<Session> sessionService,
            IMidiService midiService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.sessionService = sessionService;
            this.midiService = midiService;

            outputHandler.OutputStopped += OnPlaybackStopped;
            outputHandler.DevicesUpdated += OnDevicesUpdated;

            eventAggregator.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: ApplySettings,
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

            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public Device Device => outputHandler?.Device;

        public IEnumerable<Device> Devices => outputHandler?.Devices;

        public bool IsActive => outputHandler.IsActive;

        public bool IsLowered { get; private set; }

        public bool IsPlaying { get; private set; }

        public OutputType Type { get; private set; }

        public float Volume
        {
            get
            {
                return outputOptions?.Volume ?? default;
            }
            set
            {
                if (value != outputOptions?.Volume
                    && value >= Constants.VolumeMin
                    && value <= Constants.VolumeMax)
                {
                    outputOptions.Volume = value;

                    if (outputHandler != default)
                    {
                        outputHandler.Volume = outputOptions.Volume;
                    }
                }
            }
        }

        public WaveFormat WaveFormat { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Add(IMixerService mixerService, bool onForeground)
        {
            if (onForeground)
            {
                mixersForeground.Add(mixerService);

                foregroundProvider.Add(mixerService.Output);
            }
            else
            {
                backgroundProvider.Add(mixerService.Output);
            }

            mixersAll.Add(mixerService);
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(OutputType type)
        {
            Type = type;
        }

        public void Lower()
        {
            if (!IsLowered)
            {
                IsLowered = true;

                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                var volumeBackground = outputHandler.Volume * outputOptions.VolumePercentageBackground
                    / (float)Constants.OptionsPercentageMax;

                var volumeExternals = outputHandler.Volume * outputOptions.VolumePercentageExternals
                    / (float)Constants.OptionsPercentageMax;

                fadingProvider.Fade(
                    volumeTarget: volumeBackground,
                    milliSeconds: fadeMilliseconds);

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
            if (IsLowered)
            {
                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                fadingProvider.FadeIn(
                    milliSeconds: fadeMilliseconds);

                volumeService.Raise(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeMilliseconds);

                IsLowered = false;
            }
        }

        public void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false)
        {
            if (!IsPlaying)
            {
                outputHandler.Set(
                    deviceName: deviceName,
                    takeDefaultIfMissing: takeDefaultIfMissing);

                if (outputHandler.Device != default)
                {
                    WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                        sampleRate: sampleRate,
                        channels: outputHandler.Device.Channels);

                    InitializeProviders();

                    volumeService.Initialize(
                        device: outputHandler.Device);

                    outputOptions.DeviceName = outputHandler.Device.Name;
                    outputOptions.SampleRate = sampleRate;

                    sessionService.Save();
                }

                eventAggregator.GetEvent<DeviceUpdatedEvent>().Publish(this);
            }
        }

        public void Update()
        {
            outputHandler.Update();
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

                    mixersAll.OnEach(p => p.End());

                    mixersForeground.Clear();
                    mixersAll.Clear();

                    cancellationTokenSource?.Cancel();

                    midiService?.Deactivate();
                    midiService?.Dispose();

                    volumeService?.Deactivate();
                    volumeService?.Dispose();

                    outputHandler.OutputStopped -= OnPlaybackStopped;
                    outputHandler.DevicesUpdated -= OnDevicesUpdated;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void ApplySettings()
        {
            outputOptions = GetOptions(Type);

            volumeService = containerProvider.Resolve<IVolumeService>(
                name: Type.ToString());

            var takeDefaultIfMissing = Type == OutputType.Live;

            Set(
                deviceName: outputOptions.DeviceName,
                takeDefaultIfMissing: takeDefaultIfMissing);
        }

        private void CloseOutput()
        {
            mixersAll.OnEach(m => m.Pause());
            outputHandler?.Stop();

            if (fadingProvider != default)
            {
                fadingProvider.FadingStartedEvent -= OnFadingStarted;
                fadingProvider.FadingEndedEvent -= OnFadingEnded;
            }

            foregroundProvider?.Dispose();
            fadingProvider?.Dispose();
            backgroundProvider?.Dispose();
        }

        private Commons.Models.Settings.Output GetOptions(OutputType type)
        {
            if (sessionService?.Contents?.Outputs.Any(o => o.Type == type) != true)
            {
                var outputOptions = new Commons.Models.Settings.Output
                {
                    Type = type,
                    Volume = Constants.VolumeMax,
                };

                sessionService.Contents.Outputs.Add(outputOptions);
            }

            var result = sessionService.Contents.Outputs
                .Single(s => s.Type == type);

            return result;
        }

        private void InitializeProviders()
        {
            CloseOutput();

            backgroundProvider = new ContinuousMixingProvider(WaveFormat);

            fadingProvider = new DynamicFadingProvider(
                sourceProvider: backgroundProvider,
                volume: Constants.VolumeMax);

            fadingProvider.FadingStartedEvent += OnFadingStarted;
            fadingProvider.FadingEndedEvent += OnFadingEnded;

            foregroundProvider = new ContinuousMixingProvider(WaveFormat);
            foregroundProvider.Add(fadingProvider);

            var waveProvider = new SampleToWaveProvider(foregroundProvider);

            outputHandler.Initialize(
                waveProvider: waveProvider,
                volume: outputOptions.Volume);

            mixersAll.OnEach(mixer => Add(
                mixerService: mixer,
                onForeground: mixersForeground.Contains(mixer)));

            Task.Run(
                action: outputHandler.Play,
                cancellationToken: cancellationTokenSource.Token);
        }

        private void MuteExternals()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds;

            midiService.Mute(
                milliSeconds: fadeMilliseconds);

            volumeService.Mute(
                milliSeconds: fadeMilliseconds);
        }

        private void OnDevicesUpdated(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<DevicesUpdatedEvent>().Publish(this);
        }

        private void OnFadingEnded(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<OutputFadedEvent>().Publish(this);
        }

        private void OnFadingStarted(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<OutputFadedEvent>().Publish(this);
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
            var isPlaying = mixersAll.Any(p => p.IsPlaying);

            if (!isPlaying || mixersForeground.Contains(mixerService))
            {
                Raise();
            }

            if (IsPlaying && !isPlaying)
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

                eventAggregator.GetEvent<DeviceUpdatedEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}