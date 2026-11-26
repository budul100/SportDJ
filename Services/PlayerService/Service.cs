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
using SportDJ.Commons.Logging;
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
    private readonly IKeyboardService keyListenService;
    private readonly Lock lockSignalServices = new();
    private readonly IStorageService<Set> settingsService;
    private readonly HashSet<ISignalService> signalServices = [];
    private readonly IStackService stackService;
    private readonly List<SubscriptionToken> subscriptions = [];
    private readonly CancellationTokenSource tokenSource = new();
    private readonly ITrackService trackService;

    private volatile bool isDisposed;
    private bool isListeningMidi;
    private volatile bool isStackLoading;
    private CancellationTokenSource midiSource;
    private MixerType mixerType;
    private Player player;
    private ISignalService signalService;
    private IStorageFolder startLocation;

    #endregion Private Fields

    #region Public Constructors

    public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IStackService stackService, ITrackService trackService, IControllerService controllerService,
        IDialogService dialogService, IStorageService<Set> settingsService, IKeyboardService keyListenService)
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
        AddSubscription(eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => player != default
                && this.keyListenService?.IsListening(player) == true));

        // The controller may be connected after the keys were registered
        AddSubscription(eventAggregator?.GetEvent<ControllerUpdatedEvent>().Subscribe(
            action: ApplyLed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
            action: () => _ = SetStartLocationAsync(),
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<MixerInitializedEvent>().Subscribe(
            action: _ => InitializeSignal(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
            action: _ => UpdateDuration(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s));

        AddSubscription(eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
            action: OnSignalStarted,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s));

        AddSubscription(eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
            action: OnSignalStopped,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: IsOwnSignal));

        AddSubscription(eventAggregator?.GetEvent<SignalEndedEvent>().Subscribe(
            action: OnSignalEnded,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: IsOwnSignal));

        AddSubscription(eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
            action: _ => Next(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: s => signalService == s
                && mixerType == MixerType.Base));

        AddSubscription(eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
            action: _ => InitializeSignal(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p));

        AddSubscription(eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
            action: _ => UpdateContent(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p));

        AddSubscription(eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
            action: _ => UpdateTrack(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: t => this.stackService.Track == t));
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

    public bool IsActive => GetSignalServices().Any(s => s.State != StateType.None);

    public bool IsAvailable => signalService?.Track != default;

    public bool IsEnding => signalService?.IsEnding == true;

    public bool IsListeningKeyboard => player != default
        && keyListenService?.IsListening(player) == true;

    public bool IsListeningMidi => isListeningMidi;

    // The files are read in the background, afterwards the signal needs an initialized mixer
    public bool IsLoading => isStackLoading
        || (stackService.Track is { IsSilent: false } && !IsAvailable);

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

                // Invalid input does not notify, as the UI would write the stored name back while the user is typing.
                // The view restores the stored name, when the input loses the focus.
                UpdateContent();
            }
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
        // The settings file might have been saved elsewhere since the player was initialized
        await SetStartLocationAsync();

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
        await SetStartLocationAsync();

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
            // Set first, so handlers which are still running do not touch the released resources
            isDisposed = true;

            if (isDisposing)
            {
                // Without this the instance stays alive in the event aggregator and keeps reacting to events
                foreach (var subscription in subscriptions)
                {
                    subscription.Dispose();
                }

                subscriptions.Clear();

                keyListenService?.End(player);

                if (player != default)
                {
                    ClearLed();

                    controllerService?.Unregister(player.Key);
                }

                tokenSource?.Cancel();
                tokenSource?.Dispose();

                ReleaseSignals();
            }
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void AddSubscription(SubscriptionToken subscription)
    {
        if (subscription != default)
        {
            subscriptions.Add(subscription);
        }
    }

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

    private ISignalService[] GetSignalServices()
    {
        lock (lockSignalServices)
        {
            return [.. signalServices];
        }
    }

    private void InitializePlayer()
    {
        RegisterKey();

        var paths = player.Paths
            .Select(settingsService.GetPathFull).ToArray();

        isStackLoading = paths.Length > 0;

        _ = Task.Run(() => InitializeStack(paths));

        // TO DO: Push into DefinitionsModule/SettingsView
        // should be called after AppInitializedEvent
        _ = SetStartLocationAsync();
    }

    private void InitializeSignal()
    {
        if (isDisposed)
        {
            return;
        }

        if (stackService.Track != default)
        {
            mixerType = player.IsOverlay
                ? MixerType.Overlay
                : MixerType.Base;

            var mixerService = containerProvider?.Resolve<IMixerService>(
                name: mixerType.ToString());

            if (mixerService.IsInitialized)
            {
                var silentServices = GetSignalServices()
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

        // The loading state might have ended
        UpdateContent();
    }

    private void InitializeStack(string[] paths)
    {
        if (isDisposed)
        {
            return;
        }

        try
        {
            stackService.Initialize(
                player: player,
                paths: paths);
        }
        finally
        {
            // Also after an error, otherwise the player would show loading forever
            isStackLoading = false;

            UpdateContent();
        }
    }

    // Used as event filter, which runs on the thread of the publisher
    private bool IsOwnSignal(ISignalService signalService)
    {
        lock (lockSignalServices)
        {
            return signalServices.Contains(signalService);
        }
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
                lock (lockSignalServices)
                {
                    signalServices.Remove(signalService);
                }

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

            lock (lockSignalServices)
            {
                signalServices.Add(signalService);
            }
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

    // Takes all signals of this player out of their mixers and disposes them
    private void ReleaseSignals()
    {
        var signals = GetSignalServices();

        // The current signal might not have been started yet, so it is not part of the set
        if (signalService != default
            && !signals.Contains(signalService))
        {
            signals = [.. signals, signalService];
        }

        lock (lockSignalServices)
        {
            signalServices.Clear();
        }

        signalService = default;

        if (signals.Length > 0)
        {
            try
            {
                // The type of the mixer changes with IsOverlay, so a signal might belong to either one
                var mixers = new[] { MixerType.Base, MixerType.Overlay }
                    .Select(t => containerProvider?.Resolve<IMixerService>(
                        name: t.ToString())).ToArray();

                foreach (var signal in signals)
                {
                    foreach (var mixer in mixers)
                    {
                        mixer?.Remove(signal);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Removing the signals from the mixers failed.", exception);
            }
        }

        foreach (var signal in signals)
        {
            signal.Dispose();
        }
    }

    private async Task SetStartLocationAsync()
    {
        var paths = player?.Paths?
            .Select(settingsService.GetPathFull).ToArray() ?? [];

        // Without usable paths of the player the dialogs start where the settings file is
        var hasFolder = paths
            .Any(p => Directory.Exists(p)
                || Directory.Exists(Path.GetDirectoryName(p)));

        // The storage provider belongs to the UI thread, so no Task.Run here
        try
        {
            startLocation = hasFolder
                ? await dialogService.GetFolderAsync(paths)
                : await dialogService.GetFolderAsync(Path.GetDirectoryName(settingsService.Path));
        }
        catch (Exception exception)
        {
            Log.Warning("Determining the start folder of the dialogs failed.", exception);

            startLocation = default;
        }
    }

    private void UpdateContent()
    {
        if (isDisposed)
        {
            return;
        }

        eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);
    }

    private void UpdateDuration()
    {
        if (isDisposed)
        {
            return;
        }

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
