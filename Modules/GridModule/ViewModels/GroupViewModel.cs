using System.Collections.ObjectModel;
using System.Linq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels;

public class GroupViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly GridViewModel parent;

    private GridViewModel selected;

    #endregion Private Fields

    #region Public Constructors

    public GroupViewModel(IContainerProvider containerProvider, Group group, GridViewModel parent, bool isOnCanvas)
        : base(containerProvider: containerProvider, element: group)
    {
        this.parent = parent;

        AddSubscription(eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => IsVisible));

        InitializeTabs(
            containerProvider: containerProvider,
            group: group,
            isOnCanvas: isOnCanvas);
    }

    #endregion Public Constructors

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

    #region Protected Methods

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var tab in Tabs.ToArray())
            {
                tab.Dispose();
            }
        }

        base.Dispose(
            isDisposing: isDisposing);
    }

    #endregion Protected Methods

    #region Private Methods

    private void InitializeTabs(IContainerProvider containerProvider, Group group, bool isOnCanvas)
    {
        Rows = 1;
        Columns = 1;

        if (group?.Grids?.Count > 0)
        {
            foreach (var grid in group.Grids)
            {
                var gridTab = new GridViewModel(
                    containerProvider: containerProvider,
                    grid: grid,
                    parent: this);

                Tabs.Add(gridTab);
            }

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
            Selected = Tabs.First();
        }

        RowSpan = Rows;
        ColumnSpan = Columns;

        base.Initialize();
    }

    private void OnKeyPressed(string keyCode)
    {
        // The key assignment is started in the menu of a tab, so the tab has to receive the key
        var listeningTab = Tabs
            .FirstOrDefault(t => t.IsListening);

        if (listeningTab != default)
        {
            listeningTab.ReceiveKey(keyCode);
        }
        else if (!IsAnyListening)
        {
            SelectTab(keyCode);
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
