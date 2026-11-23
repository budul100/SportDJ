using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SportDJ.Commons.Events.Aggregation;
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

    private readonly IDispatcherService dispatcherService;
    private readonly IEventAggregator eventAggregator;
    private readonly LayoutViewModel layoutViewModel;
    private readonly IPanelService panelService;
    private readonly Dictionary<Type, TabViewModel> registrations = [];
    private readonly SettingsViewModel settingsViewModel;
    private readonly StackViewModel stackViewModel;

    private TabViewModel active;

    #endregion Private Fields

    #region Public Constructors

    public DefinitionsViewModel(IEventAggregator eventAggregator, IPanelService panelService,
        IDispatcherService dispatcherService,
        SettingsViewModel settingsViewModel, StackViewModel stackViewModel, LayoutViewModel layoutViewModel)
    {
        this.dispatcherService = dispatcherService;
        this.eventAggregator = eventAggregator;
        this.layoutViewModel = layoutViewModel;
        this.panelService = panelService;
        this.settingsViewModel = settingsViewModel;
        this.stackViewModel = stackViewModel;

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

        // The player and the grid tabs have no own pane, the tab is chosen by the activation
        registrations = new Dictionary<Type, TabViewModel>
        {
            { typeof(IStackService), stackViewModel },
            { typeof(IGridService), layoutViewModel }
        };

        // Fixed order: content, settings, layout
        Tabs = new ObservableCollection<TabViewModel>(
        [
            stackViewModel,
            settingsViewModel,
            layoutViewModel
        ]);

        Active = Tabs[0];
    }

    #endregion Public Constructors

    #region Public Properties

    public TabViewModel Active
    {
        get => active;
        set => SetProperty(ref active, value);
    }

    public DelegateCommand CloseCommand { get; }

    public ObservableCollection<TabViewModel> Tabs { get; }

    #endregion Public Properties

    #region Public Methods

    // Services without a registered tab keep the tab which is currently shown
    public void Activate<TService>()
    {
        if (registrations.TryGetValue(
            key: typeof(TService),
            value: out var registration))
        {
            Active = registration;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void CancelListening()
    {
        settingsViewModel.Cancel();
        layoutViewModel.Cancel();
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

    private void SetPlayer(Player player)
    {
        // Closing publishes no player
        if (player != default)
        {
            settingsViewModel.Set(player);
            stackViewModel.Set(player);
            layoutViewModel.SetCell(player);
        }
    }

    private void SetTab(Grid tab)
    {
        // A tab has neither content nor player settings
        settingsViewModel.Set(default);
        stackViewModel.Set(default);

        layoutViewModel.SetTab(tab);
    }

    private void Validate()
    {
        settingsViewModel.Validate();
        layoutViewModel.Validate();

        var player = settingsViewModel.Current;

        if (player == default)
        {
            stackViewModel.Set(default);
        }

        if (player == default
            && !layoutViewModel.IsActive)
        {
            Close();
        }
        else if (player != default
            && panelService?.IsVisible<IPlayerService>() == true)
        {
            // The players are created anew, so they have to learn again that their pane is open
            dispatcherService?.Post(() => eventAggregator?.GetEvent<PlayerVisibleEvent>().Publish(player));
        }
    }

    #endregion Private Methods
}
