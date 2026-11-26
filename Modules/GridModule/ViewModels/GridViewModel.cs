using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public class GridViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly ViewModelCache cache;
    private readonly Grid grid;
    private readonly IPanelService panelService;

    private int columns;
    private bool isSelected;
    private bool isSettingsVisible;
    private GroupViewModel parent;
    private int rows;

    #endregion Private Fields

    #region Public Constructors

    public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
        : this(
            containerProvider: containerProvider,
            cache: new ViewModelCache(),
            grid: grid,
            parent: parent)
    { }

    #endregion Public Constructors

    #region Internal Constructors

    internal GridViewModel(IContainerProvider containerProvider, ViewModelCache cache, Grid grid,
        GroupViewModel parent)
        : base(containerProvider: containerProvider, element: grid)
    {
        this.cache = cache;
        this.grid = grid;
        this.parent = parent;

        panelService = containerProvider?.Resolve<IPanelService>();

        ShowSettingsCommand = new DelegateCommand(
            executeMethod: ShowSettings);

        AddSubscription(eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => UpdateTab(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: g => g == this.grid));

        // The pane of the definitions shows one element at a time
        AddSubscription(eventAggregator?.GetEvent<TabVisibleEvent>().Subscribe(
            action: g => IsSettingsVisible = g == this.grid,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: _ => IsSettingsVisible = false,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        AddSubscription(eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: _ => UpdatePanelButtons(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true));

        Update();

        Initialize();
    }

    #endregion Internal Constructors

    #region Public Properties

    public ObservableCollection<CellViewModel> Cells { get; } = [];

    public int Columns
    {
        get { return columns; }
        set { SetProperty(ref columns, value); }
    }

    public bool IsSelected
    {
        get
        {
            return isSelected || parent == default;
        }
        set
        {
            isSelected = value;
        }
    }

    public bool IsSettingsVisible
    {
        get => isSettingsVisible;
        set => SetProperty(ref isSettingsVisible, value);
    }

    public bool IsVisible => IsSelected
        && parent?.IsVisible != false;

    /// <summary>
    /// The tab of the structure this view model shows.
    /// </summary>
    public Grid Model => grid;

    public int Rows
    {
        get { return rows; }
        set { SetProperty(ref rows, value); }
    }

    public DelegateCommand ShowSettingsCommand { get; }

    #endregion Public Properties

    #region Internal Methods

    // Makes this tab the visible one, also if its group is inside of a tab, which is not visible
    internal void Reveal()
    {
        parent?.Show(this);
    }

    internal void SetParent(GroupViewModel parent)
    {
        this.parent = parent;
    }

    // Adjusts the cells to the structure: view models of known models are kept, new ones are created
    // The ones which are gone are disposed by the cache afterwards
    internal void Update()
    {
        var desired = new List<CellViewModel>();

        var rowsCount = 0;
        var columnsCount = 0;

        if (grid?.Cells?.Count > 0)
        {
            var isOnCanvas = parent == default
                && grid.Cells.Count == 1;

            foreach (var element in grid.Cells.ToArray())
            {
                switch (element)
                {
                    case Player player:
                        {
                            var playerCell = cache.Get<PlayerViewModel>(player);

                            if (playerCell == default)
                            {
                                playerCell = new PlayerViewModel(
                                    containerProvider: containerProvider,
                                    player: player,
                                    parent: this);

                                cache.Add(
                                    model: player,
                                    viewModel: playerCell);
                            }
                            else
                            {
                                playerCell.SetParent(this);

                                cache.Use(player);

                                playerCell.RefreshPosition();
                            }

                            desired.Add(playerCell);

                            rowsCount = Math.Max(rowsCount, element.Row + 1);
                            columnsCount = Math.Max(columnsCount, element.Column + 1);

                            break;
                        }

                    case Group group:
                        {
                            var groupCell = cache.Get<GroupViewModel>(group);

                            if (groupCell == default)
                            {
                                groupCell = new GroupViewModel(
                                    containerProvider: containerProvider,
                                    cache: cache,
                                    group: group,
                                    parent: this,
                                    isOnCanvas: isOnCanvas);

                                cache.Add(
                                    model: group,
                                    viewModel: groupCell);
                            }
                            else
                            {
                                groupCell.SetParent(this);

                                cache.Use(group);

                                groupCell.Update(isOnCanvas);

                                groupCell.RefreshPosition();
                            }

                            desired.Add(groupCell);

                            rowsCount = Math.Max(rowsCount, groupCell.Row + groupCell.Rows);
                            columnsCount = Math.Max(columnsCount, groupCell.Column + groupCell.Columns);

                            break;
                        }
                }
            }
        }

        ViewModelCache.Synchronize(
            target: Cells,
            desired: desired);

        Rows = rowsCount;
        Columns = columnsCount;
    }

    #endregion Internal Methods

    #region Private Methods

    // One pane for all definitions, the same button opens and closes it like at a player
    private void ShowSettings()
    {
        if (IsSettingsVisible)
        {
            panelService?.Deactivate<IGridService>();
        }
        else
        {
            // The tab, whose settings are shown, is the selected one as well
            Reveal();

            eventAggregator?.GetEvent<TabVisibleEvent>().Publish(grid);

            panelService?.Activate<IGridService>();
        }
    }

    private void UpdatePanelButtons()
    {
        if (panelService?.IsVisible<IPlayerService>() != true)
        {
            IsSettingsVisible = false;
        }
    }

    private void UpdateTab()
    {
        RaisePropertyChanged(nameof(HasKey));
        RaisePropertyChanged(nameof(Key));
        RaisePropertyChanged(nameof(KeyCode));
        RaisePropertyChanged(nameof(Name));
    }

    #endregion Private Methods
}
