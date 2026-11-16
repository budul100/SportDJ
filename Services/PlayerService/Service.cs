using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DryIoc.ImTools;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
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
    private readonly IStorageService<Set> settingsService;
    private readonly HashSet<ISignalService> signalServices = [];
    private readonly IStackService stackService;
    private readonly CancellationTokenSource tokenSource = new();
    private readonly ITrackService trackService;

    private bool isDisposed;
    private MixerType mixerType;
    private Player player;
    private ISignalService signalService;
    private IStorageFolder startLocation;

    #endregion Private Fields

    #region Public Constructors

    public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IStackService stackService, ITrackService trackService, IControllerService controllerService,
        IDialogService dialogService, IStorageService<Set> settingsService)
    {
        this.containerProvider = containerProvider;
        this.eventAggregator = eventAggregator;
        this.trackService = trackService;
        this.controllerService = controllerService;
        this.dialogService = dialogService;
        this.settingsService = settingsService;
        this.stackService = stackService;

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
                && mixerType == MixerType.Background);

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

    public IEnumerable<Entry> Entries => stackService.Entries;

    public bool HasPaths => player?.Paths?.Count > 0;

    public bool HasPlayeds => stackService.HasPlayeds;

    public bool HasStack => Entries?.Count() > 1;

    public bool HasUnplayeds => stackService.HasUnplayeds;

    public bool IsActive => signalServices?.Any(s => s.State != StateType.None) == true;

    public bool IsAvailable => signalService?.Track != default;

    public bool IsEnding => signalService?.IsEnding == true;

    public bool IsForeground
    {
        get => player?.IsForeground == true;

        set
        {
            if (player != default
                && player.IsForeground != value) //&& mixerType != MixerType.PFL)
            {
                player.IsForeground = value;

                settingsService.Save();

                InitializeSignal();
            }
        }
    }

    public bool IsPlaying => signalService?.IsPlaying == true;

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
            }
        }
    }

    public Track Track => stackService.Track;

    #endregion Public Properties

    #region Public Methods

    public async Task ClearAsync()
    {
        if (HasPaths)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.PlayerResetMessage,
                contentTitle: Texts.PlayerResetHeader,
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
        player.Key = await controllerService.ListenAsync(
            cancellationToken: tokenSource.Token);

        settingsService.Save();

        eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);
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
                tokenSource?.Cancel();
                tokenSource?.Dispose();

                signalService?.Dispose();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

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
            mixerType = player.IsForeground
                ? MixerType.Foreground
                : MixerType.Background;

            var mixerService = containerProvider?.Resolve<IMixerService>(
                name: mixerType.ToString());

            if (mixerService.IsInitialized)
            {
                signalServices
                    .Where(s => s.IsSilent)
                    .OnEach(s => s.End());

                signalService = mixerService?.Add(stackService.Track);

                UpdateDuration();

                eventAggregator.GetEvent<PlayerInitializedEvent>().Publish(player);
            }
        }

        trackService?.Update(stackService.Track);
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
            || IsForeground))
        {
            signalService.End();
        }

        UpdateContent();
    }

    private void RegisterKey()
    {
        controllerService.Register(
            key: player.Key,
            strokeType: StrokeType.Down,
            action: Execute);
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