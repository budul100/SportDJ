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
        private readonly Provider outputProvider = new();
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private DynamicFadingProvider backgroundFader;
        private ContinuousMixingProvider backgroundMixer;
        private ContinuousMixingProvider foregroundMixer;
        private bool isDisposed;
        private Output outputOptions;
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
        }

        #endregion Public Constructors

        #region Public Properties

        public Device Device => outputProvider?.Device;

        public IEnumerable<Device> Devices => outputProvider?.Devices.Values;

        public WaveFormat Format { get; private set; }

        public bool IsActive => outputProvider.IsActive;

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

                    if (outputProvider != default)
                    {
                        outputProvider.Volume = outputOptions.Volume;
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

                foregroundMixer.Add(mixerService.Output);
            }
            else
            {
                backgroundMixer.Add(mixerService.Output);
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

                var volumeBackground = outputProvider.Volume * outputOptions.VolumePercentageBackground
                    / (float)Constants.OptionsPercentageMax;

                var volumeExternals = outputProvider.Volume * outputOptions.VolumePercentageExternals
                    / (float)Constants.OptionsPercentageMax;

                backgroundFader.Fade(
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

                backgroundFader.Fade(
                    volumeTarget: Constants.VolumeMax,
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
                outputProvider.Set(
                    deviceName: deviceName,
                    takeDefaultIfMissing: takeDefaultIfMissing);

                if (outputProvider.Device != default)
                {
                    Format = WaveFormat.CreateIeeeFloatWaveFormat(
                        sampleRate: sampleRate,
                        channels: outputProvider.Device.Channels);

                    InitializeProvider();

                    volumeService.Initialize(
                        device: outputProvider.Device);

                    outputOptions.DeviceName = outputProvider.Device.Name;
                    outputOptions.SampleRate = sampleRate;

                    sessionService.Save();
                }

                eventAggregator.GetEvent<OutputUpdatedEvent>().Publish(this);
            }
        }

        public void Update()
        {
            outputProvider.Update();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    mixersAll.OnEach(p => p.End());

                    mixersForeground.Clear();
                    mixersAll.Clear();

                    cancellationTokenSource?.Cancel();

                    outputProvider?.Stop();

                    foregroundMixer?.Dispose();
                    backgroundFader?.Dispose();
                    backgroundMixer?.Dispose();

                    midiService?.Deactivate();
                    midiService?.Dispose();

                    volumeService?.Deactivate();
                    volumeService?.Dispose();
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
            outputProvider?.Stop();

            if (backgroundFader != default)
            {
                backgroundFader.FadingStartedEvent -= OnFadingStarted;
                backgroundFader.FadingEndedEvent -= OnFadingEnded;
            }

            foregroundMixer?.Dispose();
            backgroundFader?.Dispose();
            backgroundMixer?.Dispose();
        }

        private Output GetOptions(OutputType type)
        {
            if (sessionService?.Contents?.Outputs.Any(o => o.Type == type) != true)
            {
                var outputOptions = new Output
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

        private void InitializeProvider()
        {
            CloseOutput();

            backgroundMixer = new ContinuousMixingProvider(Format);

            backgroundFader = new DynamicFadingProvider(
                sourceProvider: backgroundMixer,
                volume: Constants.VolumeMax);

            backgroundFader.FadingStartedEvent += OnFadingStarted;
            backgroundFader.FadingEndedEvent += OnFadingEnded;

            foregroundMixer = new ContinuousMixingProvider(Format);
            foregroundMixer.Add(backgroundFader);

            var providerWave = new SampleToWaveProvider(foregroundMixer);

            outputProvider.Initialize(
                waveProvider: providerWave,
                volume: outputOptions.Volume);

            mixersAll.OnEach(mixer => Add(
                mixerService: mixer,
                onForeground: mixersForeground.Contains(mixer)));

            Task.Run(
                action: outputProvider.Play,
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

            if (!isPlaying)
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

        #endregion Private Methods
    }
}