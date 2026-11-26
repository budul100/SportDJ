using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels;

public class GroupViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly ViewModelCache cache;
    private readonly Group group;

    private GridViewModel parent;
    private GridViewModel selected;

    #endregion Private Fields

    #region Public Constructors

    public GroupViewModel(IContainerProvider containerProvider, Group group, GridViewModel parent, bool isOnCanvas)
        : this(
            containerProvider: containerProvider,
            cache: new ViewModelCache(),
            group: group,
            parent: parent,
            isOnCanvas: isOnCanvas)
    { }

    #endregion Public Constructors

    #region Internal Constructors

    internal GroupViewModel(IContainerProvider containerProvider, ViewModelCache cache, Group group,
        GridViewModel parent, bool isOnCanvas)
        : base(containerProvider: containerProvider, element: group)
    {
        this.cache = cache;
        this.group = group;
        this.parent = parent;

        AddSubscription(eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => IsVisible));

        // A tab, which was moved, is shown as well
        AddSubscription(eventAggregator?.GetEvent<TabMovedEvent>().Subscribe(
            action: OnTabAdded,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true));

        // The MIDI key of a tab selects it as well
        AddSubscription(eventAggregator?.GetEvent<TabKeyPressedEvent>().Subscribe(
            action: OnTabKeyPressed,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true));

        // The new tab is shown, so the user sees what was just created
        AddSubscription(eventAggregator?.GetEvent<TabAddedEvent>().Subscribe(
            action: OnTabAdded,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true));

        Update(isOnCanvas);

        Initialize();
    }

    #endregion Internal Constructors

    #region Public Properties

    public int Columns { get; private set; }

    public bool IsVisible => parent?.IsVisible == true;

    public int Rows { get; private set; }

    public GridViewModel Selected
    {
        get { return selected; }
        set
        {
            if (value != default
                && value != selected)
            {
                SetProperty(ref selected, value);

                foreach (var tab in Tabs)
                {
                    tab.IsSelected = tab == value;
                }
            }
        }
    }

    public ObservableCollection<GridViewModel> Tabs { get; } = [];

    #endregion Public Properties

    #region Internal Methods

    internal void SetParent(GridViewModel parent)
    {
        this.parent = parent;
    }

    // Selects the tab and makes sure this group is visible as well
    internal void Show(GridViewModel tab)
    {
        Selected = tab;

        parent?.Reveal();
    }

    // Adjusts the tabs to the structure: view models of known tabs are kept (and with them the selection)
    internal void Update(bool isOnCanvas)
    {
        var desired = new List<GridViewModel>();

        Rows = 1;
        Columns = 1;

        if (group?.Grids?.Count > 0)
        {
            foreach (var grid in group.Grids.ToArray())
            {
                var gridTab = cache.Get<GridViewModel>(grid);

                if (gridTab == default)
                {
                    gridTab = new GridViewModel(
                        containerProvider: containerProvider,
                        cache: cache,
                        grid: grid,
                        parent: this);

                    cache.Add(
                        model: grid,
                        viewModel: gridTab);
                }
                else
                {
                    gridTab.SetParent(this);

                    cache.Use(grid);

                    gridTab.Update();
                }

                desired.Add(gridTab);
            }

            ViewModelCache.Synchronize(
                target: Tabs,
                desired: desired);

            if (!isOnCanvas)
            {
                Rows = Tabs.Max(t => t.Rows);
                Columns = Tabs.Max(t => t.Columns);

                foreach (var currentTab in Tabs)
                {
                    currentTab.Rows = Rows;
                    currentTab.Columns = Columns;
                }
            }

            // The selection must not depend on the view, otherwise no tab is visible
            // (and no key is processed) until the TabControl is realized
            // A tab which is still there stays selected
            if (selected == default
                || !Tabs.Contains(selected))
            {
                Selected = Tabs.First();
            }
        }
        else
        {
            Tabs.Clear();
        }

        RowSpan = Rows;
        ColumnSpan = Columns;
    }

    #endregion Internal Methods

    #region Private Methods

    private void OnKeyPressed(string keyCode)
    {
        // While anything waits for a key to be assigned, the key must not select a tab
        if (!IsAnyListening)
        {
            SelectTab(keyCode);
        }
    }

    private void OnTabAdded(Grid tab)
    {
        var added = Tabs
            .FirstOrDefault(t => t.Model == tab);

        if (added != default)
        {
            Show(added);
        }
    }

    private void OnTabKeyPressed(Grid tab)
    {
        if (IsVisible
            && !IsAnyListening)
        {
            var selection = Tabs
                .FirstOrDefault(t => t.Model == tab);

            if (selection != default)
            {
                Selected = selection;
            }
        }
    }

    private void SelectTab(string keyCode)
    {
        if (IsVisible)
        {
            var selection = Tabs
                .FirstOrDefault(t => t.KeyCode == keyCode);

            Selected = selection;
        }
    }

    #endregion Private Methods
}
