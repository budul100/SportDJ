using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly IContainerProvider containerProvider;
        private readonly IDeviceService deviceService;
        private readonly IEventAggregator eventAggregator;
        private readonly HashSet<IMixerService> mixersAll = [];
        private readonly HashSet<IMixerService> mixersForeground = [];
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private MixingProvider backgroundProvider;
        private FadingProvider fadingProvider;
        private MixingProvider foregroundProvider;
        private bool isDisposed;
        private IEnumerable<IMonitorService> monitorServices;
        private Output outputOptions;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService, IStorageService<Session> sessionService,
            IDeviceService deviceService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.sessionService = sessionService;
            this.deviceService = deviceService;

            deviceService.OutputStopped += OnPlaybackStopped;
            deviceService.DevicesUpdated += OnDevicesUpdated;

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

        public Device Device => deviceService?.Device;

        public IEnumerable<Device> Devices => deviceService?.Devices;

        public bool IsActive => deviceService?.IsActive == true;

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

                    if (deviceService != default)
                    {
                        deviceService.Volume = outputOptions.Volume;
                    }
                }
            }
        }

        public WaveFormat WaveFormat => deviceService?.WaveFormat;

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

            if (Type == OutputType.Live)
            {
                monitorServices = containerProvider.Resolve<IEnumerable<IMonitorService>>();

                InitializeMonitors();
            }
        }

        public void Lower()
        {
            if (!IsLowered)
            {
                IsLowered = true;

                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                var volumeBackground = deviceService.Volume * outputOptions.VolumePercentageBackground
                    / (float)Constants.OptionsPercentageMax;

                var volumeExternals = deviceService.Volume * outputOptions.VolumePercentageExternals
                    / (float)Constants.OptionsPercentageMax;

                fadingProvider.Fade(
                    volumeTarget: volumeBackground,
                    milliSeconds: fadeMilliseconds);

                InvokeOnMonitors(s => s.Reduce(
                    volumeTarget: volumeExternals,
                    milliSeconds: fadeMilliseconds));
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

                InvokeOnMonitors(s => s.Raise(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeMilliseconds));

                IsLowered = false;
            }
        }

        public void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false)
        {
            if (!IsPlaying)
            {
                deviceService.Set(
                    deviceName: deviceName,
                    sampleRate: sampleRate,
                    takeDefaultIfMissing: takeDefaultIfMissing);

                if (deviceService.Device != default)
                {
                    InitializeProviders();
                    InitializeMonitors();

                    outputOptions.DeviceName = deviceService.Device.Name;
                    outputOptions.SampleRate = sampleRate;

                    sessionService.Save();
                }

                eventAggregator.GetEvent<DeviceUpdatedEvent>().Publish(this);
            }
        }

        public void Update()
        {
            deviceService?.Update();
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

                    InvokeOnMonitors(s => s.Deactivate());
                    InvokeOnMonitors(s => s.Dispose());

                    deviceService.OutputStopped -= OnPlaybackStopped;
                    deviceService.DevicesUpdated -= OnDevicesUpdated;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void ApplySettings()
        {
            outputOptions = GetOptions(Type);

            var takeDefaultIfMissing = Type == OutputType.Live;

            Set(
                deviceName: outputOptions.DeviceName,
                takeDefaultIfMissing: takeDefaultIfMissing);
        }

        private void CloseOutput()
        {
            mixersAll.OnEach(m => m.Pause());
            deviceService?.Stop();

            if (fadingProvider != default)
            {
                fadingProvider.FadingStartedEvent -= OnFadingStarted;
                fadingProvider.FadingEndedEvent -= OnFadingEnded;
            }

            foregroundProvider?.Dispose();
            fadingProvider?.Dispose();
            backgroundProvider?.Dispose();
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

        private void InitializeMonitors()
        {
            if (!IsPlaying)
            {
                InvokeOnMonitors(s => s.Initialize(
                    device: deviceService.Device));
            }
        }

        private void InitializeProviders()
        {
            CloseOutput();

            backgroundProvider = new MixingProvider(deviceService.WaveFormat);

            fadingProvider = new FadingProvider(
                sourceProvider: backgroundProvider,
                volume: Constants.VolumeMax);

            fadingProvider.FadingStartedEvent += OnFadingStarted;
            fadingProvider.FadingEndedEvent += OnFadingEnded;

            foregroundProvider = new MixingProvider(deviceService.WaveFormat);
            foregroundProvider.Add(fadingProvider);

            deviceService.Initialize(
                sampleProvider: foregroundProvider,
                volume: outputOptions.Volume);

            mixersAll.OnEach(mixer => Add(
                mixerService: mixer,
                onForeground: mixersForeground.Contains(mixer)));

            Task.Run(
                action: deviceService.Play,
                cancellationToken: cancellationTokenSource.Token);
        }

        private void InvokeOnMonitors(Action<IMonitorService> action)
        {
            if (monitorServices?.Any() == true)
            {
                foreach (var volumeService in monitorServices)
                {
                    action(volumeService);
                }
            }
        }

        private void MuteExternals()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds;

            InvokeOnMonitors(s => s.Mute(
                milliSeconds: fadeMilliseconds));
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