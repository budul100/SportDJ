using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Logging;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.OutputService;

public class Service
    : IOutputService
{
    #region Private Fields

    private const int InitialUpdateWaitSeconds = 15;

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly IContainerProvider containerProvider;
    private readonly IDeviceService deviceService;
    private readonly IEventAggregator eventAggregator;
    private readonly Task initialUpdate;
    private readonly Lock lockMixers = new();
    private readonly HashSet<IMixerService> mixersAll = [];
    private readonly HashSet<IMixerService> mixersOverlay = [];
    private readonly IStorageService<Session> sessionService;
    private readonly IStorageService<Set> settingsService;

    private bool isDisposed;
    private IEnumerable<IMonitorService> monitorServices;
    private Output outputOptions;

    private MixingProvider providerBase;
    private FadingProvider providerFading;
    private MixingProvider providerOverlay;

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

        eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
            action: InitializeOutput,
            keepSubscriberReferenceAlive: true);

        eventAggregator.GetEvent<SettingsLoadedEvent<Session>>().Subscribe(
            action: InitializeOutput,
            keepSubscriberReferenceAlive: true);

        eventAggregator.GetEvent<DeviceStoppedEvent>().Subscribe(
            action: CloseDevice,
            keepSubscriberReferenceAlive: true);

        eventAggregator.GetEvent<MixerStartedEvent>().Subscribe(
            action: OnMixerStarted,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: IsKnownMixer);

        eventAggregator.GetEvent<MixerStoppedEvent>().Subscribe(
            action: OnMixerStopped,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: IsKnownMixer);

        eventAggregator.GetEvent<MixerEndedEvent>().Subscribe(
            action: OnMixerEnded,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: IsKnownMixer);

        // Listing the audio devices takes a while (ASIO drivers), so it runs while the windows are created
        initialUpdate = Task.Run(UpdateInitially);
    }

    #endregion Public Constructors

    #region Public Properties

    public Device Device
    {
        get
        {
            WaitForInitialUpdate();

            return deviceService?.Device;
        }
    }

    public IEnumerable<Device> Devices
    {
        get
        {
            WaitForInitialUpdate();

            return deviceService?.Devices;
        }
    }

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

    public void Add(IMixerService mixerService, bool isOverlay)
    {
        lock (lockMixers)
        {
            if (isOverlay)
            {
                mixersOverlay.Add(mixerService);
            }

            mixersAll.Add(mixerService);
        }

        // Without a device there are no providers, the mixer is added when the device is opened
        if (isOverlay)
        {
            providerOverlay?.Add(mixerService.Output);
        }
        else
        {
            providerBase?.Add(mixerService.Output);
        }
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

            providerFading?.Fade(
                volumeTarget: volumeBackground,
                milliSeconds: fadeMilliseconds);

            InvokeOnMonitors(s => s.Reduce(
                volumeTarget: volumeExternals,
                milliSeconds: fadeMilliseconds));
        }
    }

    public void Pause()
    {
        GetMixers().OnEach(p => p.Pause());

        MuteExternals();
    }

    public void Raise()
    {
        if (IsLowered)
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

            providerFading?.FadeIn(
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
            var device = Device;

            var failedNames = new HashSet<string>();

            var isOpened = OpenDevice(
                deviceName: deviceName,
                sampleRate: sampleRate,
                takeDefaultIfMissing: takeDefaultIfMissing,
                failedNames: failedNames);

            // If the default is not usable, the other devices are tried one after the other
            while (!isOpened && takeDefaultIfMissing)
            {
                var candidate = Devices?
                    .FirstOrDefault(d => !failedNames.Contains(d.Name));

                if (candidate == default)
                {
                    break;
                }

                isOpened = OpenDevice(
                    deviceName: candidate.Name,
                    sampleRate: sampleRate,
                    takeDefaultIfMissing: false,
                    failedNames: failedNames);
            }

            // Also published after a failure, so the UI does not wait for a device which will not come
            if (Device != device
                || !isOpened)
            {
                eventAggregator.GetEvent<OutputUpdatedEvent>().Publish(this);
            }
        }
    }

    public void Update()
    {
        WaitForInitialUpdate();

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

                GetMixers().OnEach(p => p.End());

                lock (lockMixers)
                {
                    mixersOverlay.Clear();
                    mixersAll.Clear();
                }

                cancellationTokenSource?.Cancel();

                InvokeOnMonitors(s => s.Deactivate());
                InvokeOnMonitors(s => s.Dispose());
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void CloseDevice()
    {
        if (Device == default)
        {
            CloseOutput();

            outputOptions.DeviceName = default;

            eventAggregator.GetEvent<OutputUpdatedEvent>().Publish(this);
        }
    }

    private void CloseOutput()
    {
        GetMixers().OnEach(m => m.Pause());
        deviceService?.Stop();

        if (providerFading != default)
        {
            providerFading.FadingStartedEvent -= OnFadingStarted;
            providerFading.FadingEndedEvent -= OnFadingEnded;
        }

        providerOverlay?.Dispose();
        providerFading?.Dispose();
        providerBase?.Dispose();

        // A disposed provider must not be used or disposed a second time
        providerOverlay = default;
        providerFading = default;
        providerBase = default;
    }

    private IMixerService[] GetMixers()
    {
        lock (lockMixers)
        {
            return [.. mixersAll];
        }
    }

    private Output GetOptions(OutputType type)
    {
        var outputs = sessionService?.Contents?.Outputs;

        // Without a session the options are not stored, but the caller still gets usable defaults
        if (outputs == default)
        {
            return new Output
            {
                Type = type,
                Volume = Constants.VolumeMax,
            };
        }

        if (!outputs.Any(o => o.Type == type))
        {
            outputs.Add(new Output
            {
                Type = type,
                Volume = Constants.VolumeMax,
            });
        }

        return outputs.First(s => s.Type == type);
    }

    private void InitializeMonitors()
    {
        if (!IsPlaying)
        {
            InvokeOnMonitors(s => s.Initialize(
                device: deviceService.Device));
        }
    }

    private void InitializeOutput()
    {
        outputOptions = GetOptions(Type);

        var takeDefaultIfMissing = Type == OutputType.Live;

        Set(
            deviceName: outputOptions.DeviceName,
            takeDefaultIfMissing: takeDefaultIfMissing);
    }

    private bool InitializeProviders()
    {
        providerBase = new MixingProvider(deviceService.WaveFormat);

        providerFading = new FadingProvider(
            sourceProvider: providerBase,
            volume: Constants.VolumeMax);

        providerFading.FadingStartedEvent += OnFadingStarted;
        providerFading.FadingEndedEvent += OnFadingEnded;

        providerOverlay = new MixingProvider(deviceService.WaveFormat);
        providerOverlay.Add(providerFading);

        var isInitialized = deviceService.Initialize(
            provider: providerOverlay,
            volume: outputOptions.Volume);

        // Without a running device nobody reads the providers, so fades and signals would never end
        if (!isInitialized)
        {
            CloseOutput();

            return false;
        }

        GetMixers().OnEach(mixer => Add(
            mixerService: mixer,
            isOverlay: IsOverlayMixer(mixer)));

        Task.Run(
            action: deviceService.Play,
            cancellationToken: cancellationTokenSource.Token);

        return true;
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

    private bool IsKnownMixer(IMixerService mixerService)
    {
        lock (lockMixers)
        {
            return mixersAll.Contains(mixerService);
        }
    }

    private bool IsOverlayMixer(IMixerService mixerService)
    {
        lock (lockMixers)
        {
            return mixersOverlay.Contains(mixerService);
        }
    }

    private void MuteExternals()
    {
        var fadeMilliseconds = settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds;

        InvokeOnMonitors(s => s.Mute(
            milliSeconds: fadeMilliseconds));
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
        lock (lockMixers)
        {
            mixersOverlay.Remove(mixerService);

            mixersAll.Remove(mixerService);
        }
    }

    private void OnMixerStarted(IMixerService mixerService)
    {
        if (!IsPlaying)
        {
            IsPlaying = true;

            eventAggregator.GetEvent<OutputStartedEvent>().Publish(this);
        }

        if (IsOverlayMixer(mixerService))
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
        var isPlaying = GetMixers().Any(p => p.IsPlaying);

        if (!isPlaying || IsOverlayMixer(mixerService))
        {
            Raise();
        }

        if (IsPlaying && !isPlaying)
        {
            IsPlaying = false;

            eventAggregator.GetEvent<OutputStoppedEvent>().Publish(this);
        }
    }

    // Returns true, if the device is open afterwards. A device which could not be initialized is added to the failed names.
    private bool OpenDevice(string deviceName, int sampleRate, bool takeDefaultIfMissing, HashSet<string> failedNames)
    {
        var previous = deviceService.Device;

        deviceService.Set(
            name: deviceName,
            sampleRate: sampleRate,
            takeDefaultIfMissing: takeDefaultIfMissing);

        var current = deviceService.Device;

        if (current == previous)
        {
            return current != default;
        }

        CloseOutput();

        if (current == default)
        {
            return false;
        }

        if (!InitializeProviders())
        {
            failedNames.Add(current.Name);

            return false;
        }

        InitializeMonitors();

        outputOptions.DeviceName = current.Name;
        outputOptions.SampleRate = sampleRate;

        sessionService.Save();

        return true;
    }

    private void UpdateInitially()
    {
        try
        {
            deviceService?.Update();
        }
        catch (Exception exception)
        {
            Log.Error("Listing the audio devices failed.", exception);
        }
    }

    private void WaitForInitialUpdate()
    {
        if (initialUpdate?.IsCompleted == false
            && !initialUpdate.Wait(TimeSpan.FromSeconds(InitialUpdateWaitSeconds)))
        {
            Log.Warning("Listing the audio devices takes longer than expected.");
        }
    }

    #endregion Private Methods
}
