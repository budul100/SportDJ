using System;
using System.Collections.Generic;
using System.Linq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class DefinitionsViewModel
    : BindableBase
{
    #region Private Fields

    private readonly ControllerSettingsViewModel controllerSettingsViewModel;
    private readonly TabViewModel[] controllerTabs;
    private readonly IDispatcherService dispatcherService;
    private readonly IEventAggregator eventAggregator;
    private readonly LayoutViewModel layoutViewModel;
    private readonly IPanelService panelService;
    private readonly TabViewModel[] playerTabs;
    private readonly Dictionary<Type, TabViewModel> registrations = [];
    private readonly SettingsViewModel settingsViewModel;
    private readonly StackViewModel stackViewModel;
    private readonly TabSettingsViewModel tabSettingsViewModel;
    private readonly TabViewModel[] tabTabs;

    private TabViewModel active;
    private bool isController;
    private bool isTab;
    private TabViewModel[] tabs;

    #endregion Private Fields

    #region Public Constructors

    public DefinitionsViewModel(IEventAggregator eventAggregator, IPanelService panelService,
        IDispatcherService dispatcherService,
        SettingsViewModel settingsViewModel, StackViewModel stackViewModel, LayoutViewModel layoutViewModel,
        TabSettingsViewModel tabSettingsViewModel, ControllerSettingsViewModel controllerSettingsViewModel)
    {
        this.controllerSettingsViewModel = controllerSettingsViewModel;
        this.dispatcherService = dispatcherService;
        this.eventAggregator = eventAggregator;
        this.layoutViewModel = layoutViewModel;
        this.panelService = panelService;
        this.settingsViewModel = settingsViewModel;
        this.stackViewModel = stackViewModel;
        this.tabSettingsViewModel = tabSettingsViewModel;

        CloseCommand = new DelegateCommand(
            executeMethod: Close);

        eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: SetPlayer,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<TabVisibleEvent>().Subscribe(
            action: SetTab,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<ControllerVisibleEvent>().Subscribe(
            action: SetController,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        // The whole grid is rebuilt after each change, so the shown element might not exist anymore
        eventAggregator?.GetEvent<GridUpdatedEvent>().Subscribe(
            action: Validate,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        // Assignments wait for a key only as long as the pane is open
        eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: _ => CancelListeningIfClosed(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        // Only the content has a pane of its own, otherwise the tab which was shown last is kept
        registrations = new Dictionary<Type, TabViewModel>
        {
            { typeof(IStackService), stackViewModel }
        };

        // Fixed order: content, settings, layout
        playerTabs =
        [
            stackViewModel,
            settingsViewModel,
            layoutViewModel
        ];

        // A tab of a group has no content
        tabTabs =
        [
            tabSettingsViewModel,
            layoutViewModel
        ];

        // The controls have no content and no layout
        controllerTabs =
        [
            controllerSettingsViewModel
        ];

        tabs = playerTabs;
        active = playerTabs[0];
    }

    #endregion Public Constructors

    #region Public Properties

    public TabViewModel Active
    {
        get => active;
        set => SetProperty(ref active, value);
    }

    public DelegateCommand CloseCommand { get; }

    public TabViewModel[] Tabs => tabs;

    #endregion Public Properties

    #region Public Methods

    // Services without a registered tab keep the tab which is currently shown
    public void Activate<TService>()
    {
        if (registrations.TryGetValue(
            key: typeof(TService),
            value: out var registration)
            && !isTab)
        {
            Active = registration;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void CancelListening()
    {
        settingsViewModel.Cancel();
        tabSettingsViewModel.Cancel();
        layoutViewModel.Cancel();
        controllerSettingsViewModel.Cancel();
    }

    private void CancelListeningIfClosed()
    {
        if (!panelService.IsVisible<IPlayerService>())
        {
            CancelListening();
        }
    }

    private void Close()
    {
        // Resets the buttons of the players and cancels pending assignments via the panel update
        panelService?.Deactivate<IPlayerService>();
    }

    private void SetController()
    {
        SetMode(false, true);

        // The controls have neither a player nor a tab
        settingsViewModel.Set(default);
        stackViewModel.Set(default);
        tabSettingsViewModel.Set(default);
    }

    // Switches between the tabs of a player, of a group tab and of the controls, the shown tab is kept if possible
    private void SetMode(bool isTab, bool isController)
    {
        if (this.isTab != isTab
            || this.isController != isController)
        {
            this.isController = isController;
            this.isTab = isTab;

            tabs = isController
                ? controllerTabs
                : isTab
                    ? tabTabs
                    : playerTabs;

            RaisePropertyChanged(nameof(Tabs));
        }

        if (!tabs.Contains(active))
        {
            Active = tabs[0];
        }
        else
        {
            // The selection of the control was reset by the new items
            RaisePropertyChanged(nameof(Active));
        }
    }

    private void SetPlayer(Player player)
    {
        // Closing publishes no player
        if (player != default)
        {
            SetMode(false, false);

            controllerSettingsViewModel.Cancel();
            tabSettingsViewModel.Set(default);

            settingsViewModel.Set(player);
            stackViewModel.Set(player);
            layoutViewModel.SetCell(player);
        }
    }

    private void SetTab(Grid tab)
    {
        // Closing publishes no tab
        if (tab != default)
        {
            SetMode(true, false);

            controllerSettingsViewModel.Cancel();

            // A tab has neither content nor player settings
            settingsViewModel.Set(default);
            stackViewModel.Set(default);

            tabSettingsViewModel.Set(tab);
            layoutViewModel.SetTab(tab);
        }
    }

    private void Validate()
    {
        settingsViewModel.Validate();
        tabSettingsViewModel.Validate();
        layoutViewModel.Validate();

        // The controls do not depend on the grid
        if (isController)
        {
            return;
        }

        var player = settingsViewModel.Current;
        var tab = tabSettingsViewModel.Current;

        if (player == default)
        {
            stackViewModel.Set(default);
        }

        if (player == default
            && tab == default
            && !layoutViewModel.IsActive)
        {
            Close();
        }
        else if (isTab
            && tab == default)
        {
            // The tab was removed
            Close();
        }
        else if (panelService?.IsVisible<IPlayerService>() == true)
        {
            // The players and tabs are created anew, so they have to learn again that their pane is open
            if (player != default)
            {
                dispatcherService?.Post(() => eventAggregator?.GetEvent<PlayerVisibleEvent>().Publish(player));
            }
            else if (tab != default)
            {
                dispatcherService?.Post(() => eventAggregator?.GetEvent<TabVisibleEvent>().Publish(tab));
            }
        }
    }

    #endregion Private Methods
}