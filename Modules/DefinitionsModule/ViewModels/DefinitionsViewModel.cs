using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class DefinitionsViewModel
    : BindableBase
{
    #region Private Fields

    private readonly Dictionary<Type, TabViewModel> registrations = [];
    private readonly SettingsViewModel settingsViewModel;
    private readonly StackViewModel stackViewModel;

    private TabViewModel active;

    #endregion Private Fields

    #region Public Constructors

    public DefinitionsViewModel(IEventAggregator eventAggregator,
        SettingsViewModel settingsViewModel, StackViewModel stackViewModel)
    {
        this.settingsViewModel = settingsViewModel;
        this.stackViewModel = stackViewModel;

        eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: SetPlayer,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
            action: SetPlayer,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        registrations = new Dictionary<Type, TabViewModel>
        {
            { typeof(IPlayerService), settingsViewModel },
            { typeof(IStackService), stackViewModel }
        };

        Tabs = new ObservableCollection<TabViewModel>(registrations.Values);

        if (Tabs.Count > 0)
        {
            Active = Tabs[0];
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public TabViewModel Active
    {
        get => active;
        set => SetProperty(ref active, value);
    }

    public ObservableCollection<TabViewModel> Tabs { get; }

    #endregion Public Properties

    #region Public Methods

    public void Activate<TService>()
    {
        var serviceType = typeof(TService);

        if (registrations.TryGetValue(
            key: serviceType,
            value: out var registration))
        {
            Active = registration;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void SetPlayer(Player player)
    {
        settingsViewModel.Set(player);
        stackViewModel.Set(player);
    }

    #endregion Private Methods
}
