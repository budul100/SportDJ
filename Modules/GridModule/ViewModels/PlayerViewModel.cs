using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Material.Icons;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Stack;
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
    private bool isStackVisible;

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

        ShowStackCommand = new DelegateCommand(
            executeMethod: ShowStack);

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

        // While any cell waits for a key to be assigned, only this cell must react
        AddSubscription(eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: k => parent?.IsVisible == true
                && (IsListening || (!IsAnyListening && k == player.KeyCode))));

        AddSubscription(eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: p => { IsSettingsVisible = p == player; IsStackVisible = false; },
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
            action: p => { IsSettingsVisible = false; IsStackVisible = p == player; },
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

    public bool HasPFL { get; private set; }

    public bool HasPFLOrKey => HasPFL || HasKey;

    public bool HasStack => playerService?.HasStack == true;

    public virtual string Info => playerService?.Track?.Info;

    public virtual bool IsActive => playerService?.IsActive == true;

    public bool IsAvailableExecute => playerService?.IsAvailable == true;

    public bool IsAvailableSelection => playerService?.HasUnplayeds == true;

    public bool IsEnding => playerService?.IsEnding == true;

    public bool IsOverlay
    {
        get { return playerService?.IsOverlay == true; }
        set
        {
            if (playerService != default
                && value != playerService.IsOverlay)
            {
                playerService.IsOverlay = value;

                RaisePropertyChanged(nameof(IsOverlay));
            }
        }
    }

    public virtual bool IsPlaying => playerService?.IsPlaying == true;

    public bool IsSettingsVisible
    {
        get => isSettingsVisible;
        set => SetProperty(ref isSettingsVisible, value);
    }

    public bool IsStackVisible
    {
        get => isStackVisible;
        set => SetProperty(ref isStackVisible, value);
    }

    public double? Progress => playerService?.Progress;

    public virtual string Remaining => playerService?.Remaining?.ToString(Constants.FormatDuration);

    public bool ResetPosition
    {
        get { return playerService?.ResetPosition == true; }
        set
        {
            if (playerService != default
                && value != playerService.ResetPosition)
            {
                playerService.ResetPosition = value;

                RaisePropertyChanged(nameof(ResetPosition));
            }
        }
    }

    public DelegateCommand ShowSettingsCommand { get; }

    public DelegateCommand ShowStackCommand { get; }

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

    protected override IEnumerable<BindableBase> GetElementMenus()
    {
        var gridMenus = GetGridMenus().ToArray();

        yield return new MenuViewModel(
            header: $"{Texts.GridEditHeader}...",
            menuItems: gridMenus,
            closeCallback: CloseMenu);

        var playerMenus = GetPlayerMenus().ToArray();

        yield return new MenuViewModel(
            header: $"{Texts.PlayerEditHeader}...",
            menuItems: playerMenus,
            closeCallback: CloseMenu);

        if (playerService?.HasPaths == true)
        {
            // Sorted by the same text, which is used to build the group headers
            var menuItems = GetMenuTracks()
                .OrderBy(m => m.Entry.Track?.Info).ToArray();

            if (menuItems.Length > 1)
            {
                yield return new SeparatorViewModel();

                var groupsSize = (menuItems.Length / ((menuItems.Length / Constants.MenuGroupBaseSize) + 1)) + 1;

                var groups = menuItems
                    .Chunk(groupsSize).ToArray();

                foreach (var group in groups)
                {
                    yield return new TrackGroupMenuViewModel(
                        eventAggregator: eventAggregator,
                        menuItems: group,
                        closeCallback: CloseMenu);
                }
            }
        }
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

    private void ExecutePlayer()
    {
        if (IsRenaming)
        {
            IsRenaming = false;
        }
        else
        {
            if (playerService?.IsAvailable == true)
            {
                playerService.Execute();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    private IEnumerable<TrackMenuViewModel> GetMenuTracks()
    {
        if (playerService?.Entries?.Any() == true)
        {
            foreach (var entry in playerService.Entries)
            {
                yield return new TrackMenuViewModel(
                    eventAggregator: eventAggregator,
                    entry: entry,
                    closeCallback: CloseMenu);
            }
        }
    }

    private IEnumerable<BindableBase> GetPlayerMenus()
    {
        yield return new MenuViewModel(
            header: $"{Texts.PlayerSelectionFileHeader}...",
            executeCallback: () => playerService?.SetFileAsync(),
            closeCallback: CloseMenu,
            icon: MaterialIconKind.FileMultipleOutline);

        yield return new MenuViewModel(
            header: $"{Texts.PlayerSelectionFolderHeader}...",
            executeCallback: () => playerService?.SetFolderAsync(),
            closeCallback: CloseMenu,
            icon: MaterialIconKind.FolderMultipleOutline);

        yield return new MenuViewModel(
            header: Texts.PlayerResetHeader,
            executeCallback: () => playerService.ClearAsync(),
            closeCallback: CloseMenu,
            icon: MaterialIconKind.CloseCircleOutline);

        yield return new SeparatorViewModel();

        yield return new MenuViewModel(
            header: Texts.PlayerRenameHeader,
            executeCallback: RenamePlayer,
            closeCallback: CloseMenu,
            icon: MaterialIconKind.FormTextbox);

        var listenItem = new MenuViewModel(
            header: Texts.KeyListenHeader,
            executeCallback: ListenHook,
            isCheckedGetter: () => IsListening,
            icon: MaterialIconKind.KeyboardOutline);

        listenChecker = listenItem.Update;

        yield return listenItem;

        yield return new MenuViewModel(
            header: Texts.KeyUnhookHeader,
            executeCallback: () => KeyCode = default,
            closeCallback: CloseMenu,
            icon: MaterialIconKind.KeyboardOffOutline);

        if (playerService?.HasPaths == true)
        {
            yield return new SeparatorViewModel();

            yield return new MenuViewModel(
                header: Texts.PlayerOverlayHeader,
                executeCallback: () => IsOverlay = !IsOverlay,
                closeCallback: CloseMenu,
                isCheckedGetter: () => IsOverlay,
                icon: MaterialIconKind.ChevronUpBoxOutline);

            yield return new MenuViewModel(
                header: Texts.PlayerResetPosition,
                executeCallback: () => ResetPosition = !ResetPosition,
                closeCallback: CloseMenu,
                isCheckedGetter: () => ResetPosition,
                icon: MaterialIconKind.FileRestoreOutline);
        }
    }

    private void OnKeyPressed(string keyCode)
    {
        if (IsListening)
        {
            ReceiveKey(keyCode);
        }
        else
        {
            ExecutePlayer();
        }
    }

    private void RenamePlayer()
    {
        if (playerService?.IsActive != true)
        {
            IsRenaming = true;
        }
    }

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

    private void ShowStack()
    {
        if (IsStackVisible || player.StackService == default)
        {
            panelService.Deactivate<IStackService>();

            eventAggregator?.GetEvent<StackVisibleEvent>().Publish(
                payload: default);
        }
        else
        {
            eventAggregator?.GetEvent<StackVisibleEvent>().Publish(
                payload: player);

            panelService.Activate<IStackService>();
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
        RaisePropertyChanged(nameof(HasPFL));
        RaisePropertyChanged(nameof(HasPFLOrKey));
        RaisePropertyChanged(nameof(HasStack));

        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(IsOverlay));
        RaisePropertyChanged(nameof(ResetPosition));

        ExecuteCommand.RaiseCanExecuteChanged();

        TrackBackwardCommand.RaiseCanExecuteChanged();
        TrackForwardCommand.RaiseCanExecuteChanged();
        ShowStackCommand.RaiseCanExecuteChanged();
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

        if (!panelService.IsVisible<IStackService>())
        {
            IsStackVisible = false;
        }
    }

    #endregion Private Methods
}
