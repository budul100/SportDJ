using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.PlayerService;

public class Service
    : IPlayerService
{
    #region Private Fields

    private readonly IContainerProvider containerProvider;
    private readonly IControllerService controllerService;
    private readonly IDialogService dialogService;
    private readonly IEventAggregator eventAggregator;
    private readonly IKeyListenService keyListenService;
    private readonly IStorageService<Set> settingsService;
    private readonly HashSet<ISignalService> signalServices = [];
    private readonly IStackService stackService;
    private readonly CancellationTokenSource tokenSource = new();
    private readonly ITrackService trackService;

    private bool isDisposed;
    private bool isListeningMidi;
    private CancellationTokenSource midiSource;
    private MixerType mixerType;
    private Player player;
    private ISignalService signalService;
    private IStorageFolder startLocation;

    #endregion Private Fields

    #region Public Constructors

    public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IStackService stackService, ITrackService trackService, IControllerService controllerService,
        IDialogService dialogService, IStorageService<Set> settingsService, IKeyListenService keyListenService)
    {
        this.containerProvider = containerProvider;
        this.eventAggregator = eventAggregator;
        this.trackService = trackService;
        this.controllerService = controllerService;
        this.dialogService = dialogService;
        this.settingsService = settingsService;
        this.stackService = stackService;
        this.keyListenService = keyListenService;

        // Only this player takes the key, while it waits for an assignment
        eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => player != default
                && this.keyListenService?.IsListening(player) == true);

        // The controller may be connected after the keys were registered
        eventAggregator?.GetEvent<ControllerUpdatedEvent>().Subscribe(
            action: ApplyLed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
            action: SetStartLocation,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<MixerInitializedEvent>().Subscribe(
            action: _ => InitializeSignal(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
            action: _ => UpdateDuration(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s);

        eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
            action: OnSignalStarted,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s);

        eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
            action: OnSignalStopped,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: signalServices.Contains);

        eventAggregator?.GetEvent<SignalEndedEvent>().Subscribe(
            action: OnSignalEnded,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: signalServices.Contains);

        eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
            action: _ => Next(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s
                && mixerType == MixerType.Base);

        eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
            action: _ => InitializeSignal(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p);

        eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
            action: _ => UpdateContent(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p);

        eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
            action: _ => UpdateTrack(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: t => this.stackService.Track == t);
    }

    #endregion Public Constructors

    #region Public Properties

    public PlayerColorType Color
    {
        get => player?.Color ?? default;

        set
        {
            if (player != default
                && player.Color != value)
            {
                player.Color = value;

                settingsService.Save();

                ApplyLed();

                UpdateContent();
            }
        }
    }

    public IEnumerable<Entry> Entries => stackService.Entries;

    public bool HasPaths => player?.Paths?.Count > 0;

    public bool HasPlayeds => stackService.HasPlayeds;

    public bool HasStack => Entries?.Count() > 1;

    public bool HasUnplayeds => stackService.HasUnplayeds;

    public bool IsActive => signalServices?.Any(s => s.State != StateType.None) == true;

    public bool IsAvailable => signalService?.Track != default;

    public bool IsEnding => signalService?.IsEnding == true;

    public bool IsOverlay
    {
        get => player?.IsOverlay == true;

        set
        {
            if (player != default
                && player.IsOverlay != value) //&& mixerType != MixerType.PFL)
            {
                player.IsOverlay = value;

                settingsService.Save();

                InitializeSignal();

                UpdateContent();
            }
        }
    }

    public bool IsListeningKeyboard => player != default
        && keyListenService?.IsListening(player) == true;

    public bool IsListeningMidi => isListeningMidi;

    public bool IsPlaying => signalService?.IsPlaying == true;

    public Key Key => player?.Key ?? default;

    public string KeyCode
    {
        get => player?.KeyCode;

        set
        {
            if (player != default
                && player.KeyCode != value)
            {
                player.KeyCode = value;

                settingsService.Save();

                UpdateContent();
            }
        }
    }

    public string Name
    {
        get => player?.Name;

        set
        {
            var trimmed = value?.Trim();

            if (player != default
                && !string.IsNullOrWhiteSpace(trimmed)
                && player.Name != trimmed)
            {
                player.Name = trimmed;

                settingsService.Save();
            }

            // Also notify for invalid input, so the UI falls back to the stored name
            UpdateContent();
        }
    }

    public double? Progress { get; private set; }

    public TimeSpan? Remaining { get; private set; }

    public bool ResetPosition
    {
        get => player?.ResetPosition == true;

        set
        {
            if (player != default
                && player.ResetPosition != value)
            {
                player.ResetPosition = value;

                settingsService.Save();

                UpdateContent();
            }
        }
    }

    public Track Track => stackService.Track;

    #endregion Public Properties

    #region Public Methods

    public void CancelListening()
    {
        CancelMidi();

        EndKeyboardListening();
    }

    public async Task ClearAsync()
    {
        if (HasPaths)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.PlayerClearMessage,
                contentTitle: Texts.PlayerClearHeader,
                buttonDefinitions: ButtonEnum.YesNo,
                enterDefaultButton: ClickEnum.No);

            if (result == ButtonResult.Yes)
            {
                player.Paths.Clear();

                settingsService.Save();

                InitializePlayer();
            }
        }
    }

    public void Dispose()
    {
        Dispose(
            isDisposing: true);

        GC.SuppressFinalize(
            obj: this);
    }

    public void Execute()
    {
        if (IsAvailable)
        {
            if (signalService.State == StateType.Play)
            {
                signalService.Pause();
            }
            else if (signalService.State != StateType.Stop)
            {
                signalService.Start();
            }
        }
    }

    public void Initialize(Player player)
    {
        this.player = player;
        player.PlayerService = this;

        InitializePlayer();
    }

    public void ListenKeyboard()
    {
        if (player != default)
        {
            CancelMidi();

            keyListenService?.Begin(player);

            UpdateContent();
        }
    }

    public void Next()
    {
        stackService.Next();
    }

    public void Previous()
    {
        stackService.Previous();
    }

    public async Task RegisterAsync()
    {
        CancelListening();

        var source = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token);

        midiSource = source;
        isListeningMidi = true;

        UpdateContent();

        try
        {
            var key = await controllerService.ListenAsync(
                cancellationToken: source.Token);

            // The old key must not trigger this player anymore
            ClearLed();

            controllerService.Unregister(player.Key);

            player.Key = key;

            // Without this the learned key only works after the next restart (InitializePlayer)
            RegisterKey();

            settingsService.Save();
        }
        catch (OperationCanceledException)
        {
            // Listening was replaced by another player, cancelled or the service was disposed
        }
        finally
        {
            // A replaced listener must not reset the state of its successor
            if (ReferenceEquals(midiSource, source))
            {
                midiSource = default;
                isListeningMidi = false;
            }

            source.Dispose();

            if (!isDisposed)
            {
                UpdateContent();
            }
        }
    }

    public async Task SetFileAsync()
    {
        var paths = await dialogService.OpenFilePickerAsync(
            title: Texts.PlayerSelectionFileHeader,
            allowMultiple: true,
            startLocation: startLocation);

        if (paths?.Any() == true)
        {
            player.Paths = paths
                .Select(p => p.Path.LocalPath)
                .Where(File.Exists)
                .Select(settingsService.GetPathRelative).ToList();

            settingsService.Save();

            InitializePlayer();
        }
    }

    public async Task SetFolderAsync()
    {
        var paths = await dialogService.OpenFolderPickerAsync(
            title: Texts.PlayerSelectionFolderHeader,
            allowMultiple: true,
            startLocation: startLocation);

        if (paths?.Any() == true)
        {
            player.Paths = paths
                .Select(p => p.Path.LocalPath)
                .Where(Directory.Exists)
                .Select(settingsService.GetPathRelative).ToList();

            settingsService.Save();

            InitializePlayer();
        }
    }

    public void Unregister()
    {
        ClearLed();

        controllerService.Unregister(player.Key);

        player.Key = default;

        settingsService.Save();

        eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool isDisposing)
    {
        if (!isDisposed)
        {
            if (isDisposing)
            {
                keyListenService?.End(player);

                tokenSource?.Cancel();
                tokenSource?.Dispose();

                signalService?.Dispose();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void ApplyLed()
    {
        if (player?.Key.Channel > 0)
        {
            controllerService.Colorize(
                key: player.Key,
                colorType: player.Color);
        }
    }

    private void CancelMidi()
    {
        try
        {
            midiSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The listener has already finished
        }
    }

    private void ClearLed()
    {
        if (player?.Key.Channel > 0)
        {
            controllerService.Colorize(
                key: player.Key,
                colorType: PlayerColorType.None);
        }
    }

    private void EndKeyboardListening()
    {
        if (IsListeningKeyboard)
        {
            keyListenService.End(player);

            UpdateContent();
        }
    }

    private void InitializePlayer()
    {
        RegisterKey();

        var paths = player.Paths
            .Select(settingsService.GetPathFull).ToArray();

        Task.Run(() => stackService.Initialize(
            player: player,
            paths: paths));

        // TO DO: Push into DefinitionsModule/SettingsView
        // should be called after AppInitializedEvent
        SetStartLocation();
    }

    private void InitializeSignal()
    {
        if (stackService.Track != default)
        {
            mixerType = player.IsOverlay
                ? MixerType.Overlay
                : MixerType.Base;

            var mixerService = containerProvider?.Resolve<IMixerService>(
                name: mixerType.ToString());

            if (mixerService.IsInitialized)
            {
                var silentServices = signalServices
                    .Where(s => s.IsSilent)
                    .ToArray();

                foreach (var silentService in silentServices)
                {
                    silentService.End();
                }

                signalService = mixerService?.Add(stackService.Track);

                UpdateDuration();

                eventAggregator.GetEvent<PlayerInitializedEvent>().Publish(player);
            }
        }

        trackService?.Update(stackService.Track);
    }

    private void OnKeyPressed(string keyCode)
    {
        KeyCode = keyCode;

        // Ends later, so other receivers of this key press still see the listening state
        Dispatcher.UIThread.Post(EndKeyboardListening);
    }

    private void OnSignalEnded(ISignalService signalService)
    {
        if (!signalService.IsLoop)
        {
            if (this.signalService == signalService
                && signalService.Track == stackService.Track)
            {
                Next();
            }

            if (signalService.IsSilent)
            {
                signalServices.Remove(signalService);

                signalService.Dispose();
            }
        }

        UpdateContent();
    }

    private void OnSignalStarted(ISignalService signalService)
    {
        if (signalService == this.signalService)
        {
            if (signalService.Track == stackService.Track)
            {
                stackService.Play();
            }

            signalServices.Add(signalService);
        }

        UpdateContent();
    }

    private void OnSignalStopped(ISignalService signalService)
    {
        if (signalService == this.signalService
            && (signalService.State == StateType.None
            || signalService.IsOverNextThreshold
            || IsOverlay))
        {
            signalService.End();
        }

        UpdateContent();
    }

    private void RegisterKey()
    {
        // Unassigned players carry the default key, which must not be registered
        if (player.Key == default)
        {
            return;
        }

        controllerService.Register(
            key: player.Key,
            strokeType: StrokeType.Down,
            action: Execute);

        ApplyLed();
    }

    private void SetStartLocation()
    {
        var paths = player?.Paths?
            .Select(settingsService.GetPathFull).ToArray();

        if (paths?.Length > 0)
        {
            Task.Run(async () => startLocation = await dialogService.GetFolderAsync(paths));
        }
    }

    private void UpdateContent()
    {
        eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);
    }

    private void UpdateDuration()
    {
        var remaining = signalService?.Track == stackService.Track
            ? signalService?.Remaining
            : stackService?.Track?.Duration;

        if (remaining?.Ticks > 0)
        {
            Remaining = remaining.Value;
            Progress = signalService?.Track == stackService.Track
                ? signalService.Progress
                : default;
        }
        else
        {
            Remaining = default;
            Progress = default;
        }

        eventAggregator.GetEvent<PlayerShiftedEvent>().Publish(player);
    }

    private void UpdateTrack()
    {
        UpdateContent();
        UpdateDuration();
    }

    #endregion Private Methods
}
