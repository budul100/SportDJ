using Avalonia.Media;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public class PlayerViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly IPanelService panelService;
    private readonly Player player;
    private readonly IPlayerService playerService;

    private bool isSettingsVisible;

    #endregion Private Fields

    #region Public Constructors

    public PlayerViewModel(IContainerProvider containerProvider, Player player, GridViewModel parent)
        : base(containerProvider: containerProvider, element: player)
    {
        this.player = player;

        panelService = containerProvider?.Resolve<IPanelService>();
        playerService = containerProvider?.Resolve<IPlayerService>();

        ExecuteCommand = new DelegateCommand(
            executeMethod: ExecutePlayer);

        TrackBackwardCommand = new DelegateCommand(
            executeMethod: () => playerService?.Previous(),
            canExecuteMethod: () => IsAvailableSelection);

        TrackForwardCommand = new DelegateCommand(
            executeMethod: () => playerService?.Next(),
            canExecuteMethod: () => IsAvailableSelection);

        ShowSettingsCommand = new DelegateCommand(
            executeMethod: ShowSettings);

        AddSubscription(eventAggregator?.GetEvent<PlayerInitializedEvent>().Subscribe(
            action: _ => Initialize(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p));

        AddSubscription(eventAggregator?.GetEvent<PlayerUpdateEvent>().Subscribe(
            action: _ => UpdateContent(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p));

        AddSubscription(eventAggregator?.GetEvent<PlayerShiftedEvent>().Subscribe(
            action: _ => UpdateDuration(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player == p));

        AddSubscription(eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
            action: _ => UpdateContent(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: o => outputServiceLive == o));

        AddSubscription(eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
            action: _ => UpdateContent(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: o => outputServiceLive == o));

        // While anything waits for a key to be assigned, the key must not trigger a player
        AddSubscription(eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: _ => ExecuteAvailable(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: k => parent?.IsVisible == true
                && !IsAnyListening
                && k == player.KeyCode));

        AddSubscription(eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: p => IsSettingsVisible = p == player,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<TabVisibleEvent>().Subscribe(
            action: _ => IsSettingsVisible = false,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: t => UpdatePanelButtons(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        playerService?.Initialize(
            player: player);

        Initialize();
    }

    #endregion Public Constructors

    #region Public Properties

    public virtual Bitmap Bitmap => playerService?.Track?.Bitmap;

    public virtual string Counter => playerService?.Track?.Counter > 0
        ? $"({playerService.Track.Counter})"
        : default;

    public DelegateCommand ExecuteCommand { get; }

    // Anything to show in the middle of the top bar: keyboard key, MIDI key, overlay
    public bool HasInfo => HasKey || HasMidi || IsOverlay;

    public bool HasMidi => !string.IsNullOrEmpty(MidiText);

    public bool HasPFL { get; private set; }

    public bool HasPFLOrKey => HasPFL || HasKey;

    public bool HasStack => playerService?.HasStack == true;

    public virtual string Info => playerService?.Track?.Info;

    public virtual bool IsActive => playerService?.IsActive == true;

    public bool IsAvailableExecute => playerService?.IsAvailable == true;

    public bool IsAvailableSelection => playerService?.HasUnplayeds == true;

    public bool IsEnding => playerService?.IsEnding == true;

    public bool IsOverlay => playerService?.IsOverlay == true;

    public virtual bool IsPlaying => playerService?.IsPlaying == true;

    public bool IsSettingsVisible
    {
        get => isSettingsVisible;
        set => SetProperty(ref isSettingsVisible, value);
    }

    // Number of the assigned MIDI key, the complete text is the tooltip
    public string MidiNumber => HasMidi
        ? playerService.Key.Number.ToString()
        : default;

    public string MidiText => playerService?.Key.GetText();

    public double? Progress => playerService?.Progress;

    public virtual string Remaining => playerService?.Remaining?.ToString(Constants.FormatDuration);

    public DelegateCommand ShowSettingsCommand { get; }

    // Idle background of the player surface
    public IBrush SurfaceBrush => (playerService?.Color ?? default).GetBrush();

    public DelegateCommand TrackBackwardCommand { get; }

    public DelegateCommand TrackForwardCommand { get; }

    #endregion Public Properties

    #region Protected Methods

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            playerService?.Dispose();
        }

        base.Dispose(
            isDisposing: isDisposing);
    }

    protected override void Initialize()
    {
        base.Initialize();

        UpdateDuration();
        UpdateContent();
    }

    protected override void UpdateOutput()
    {
        base.UpdateOutput();

        //var outputServicePFL = containerProvider?.Resolve<IOutputService>(
        //    nameof(OutputType.PFL));
        //HasPFL = outputServicePFL?.IsActive == true;
        UpdateContent();
    }

    #endregion Protected Methods

    #region Private Methods

    private void ExecuteAvailable()
    {
        if (IsRenaming)
        {
            IsRenaming = false;
        }
        else if (playerService?.IsAvailable == true)
        {
            playerService.Execute();
        }
    }

    private void ExecutePlayer()
    {
        // A click on a player without playable content leads to its content
        if (!IsRenaming
            && playerService?.IsAvailable != true)
        {
            ShowContent();
        }
        else
        {
            ExecuteAvailable();
        }
    }

    private void ShowContent()
    {
        if (player.PlayerService != default)
        {
            eventAggregator?.GetEvent<PlayerVisibleEvent>().Publish(
                payload: player);

            panelService.Activate<IStackService>();
        }
    }

    // One pane for all definitions, it keeps the tab which was shown last
    private void ShowSettings()
    {
        if (IsSettingsVisible || player.PlayerService == default)
        {
            panelService.Deactivate<IPlayerService>();

            eventAggregator?.GetEvent<PlayerVisibleEvent>().Publish(
                payload: default);
        }
        else
        {
            eventAggregator?.GetEvent<PlayerVisibleEvent>().Publish(
                payload: player);

            panelService.Activate<IPlayerService>();
        }
    }

    private void UpdateContent()
    {
        RaisePropertyChanged(nameof(Info));
        RaisePropertyChanged(nameof(Counter));
        RaisePropertyChanged(nameof(Bitmap));

        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsPlaying));
        RaisePropertyChanged(nameof(IsEnding));

        RaisePropertyChanged(nameof(IsAvailableExecute));
        RaisePropertyChanged(nameof(IsAvailableSelection));

        RaisePropertyChanged(nameof(HasKey));
        RaisePropertyChanged(nameof(HasMidi));
        RaisePropertyChanged(nameof(HasInfo));
        RaisePropertyChanged(nameof(MidiNumber));
        RaisePropertyChanged(nameof(MidiText));
        RaisePropertyChanged(nameof(HasPFL));
        RaisePropertyChanged(nameof(HasPFLOrKey));
        RaisePropertyChanged(nameof(HasStack));

        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(IsOverlay));
        RaisePropertyChanged(nameof(SurfaceBrush));
        RaisePropertyChanged(nameof(Key));
        RaisePropertyChanged(nameof(KeyCode));

        ExecuteCommand.RaiseCanExecuteChanged();

        TrackBackwardCommand.RaiseCanExecuteChanged();
        TrackForwardCommand.RaiseCanExecuteChanged();
    }

    private void UpdateDuration()
    {
        RaisePropertyChanged(nameof(Progress));
        RaisePropertyChanged(nameof(Remaining));
    }

    private void UpdatePanelButtons()
    {
        if (!panelService.IsVisible<IPlayerService>())
        {
            IsSettingsVisible = false;
        }
    }

    #endregion Private Methods
}