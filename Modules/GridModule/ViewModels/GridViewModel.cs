using System;
using System.Collections.ObjectModel;
using System.Linq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public class GridViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly Grid grid;
    private readonly IPanelService panelService;
    private readonly GroupViewModel parent;

    private int columns;
    private bool isSelected;
    private int rows;

    #endregion Private Fields

    #region Public Constructors

    public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
        : base(containerProvider: containerProvider, element: grid)
    {
        this.grid = grid;
        this.parent = parent;

        panelService = containerProvider?.Resolve<IPanelService>();

        ShowLayoutCommand = new DelegateCommand(
            executeMethod: ShowLayout);

        AddSubscription(eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => UpdateTab(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: g => g == grid));

        InitializeCells(
            containerProvider: containerProvider,
            grid: grid);
    }

    #endregion Public Constructors

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

    public bool IsVisible => IsSelected
        && parent?.IsVisible != false;

    public int Rows
    {
        get { return rows; }
        set { SetProperty(ref rows, value); }
    }

    public DelegateCommand ShowLayoutCommand { get; }

    #endregion Public Properties

    #region Protected Methods

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var child in Cells.ToArray())
            {
                child.Dispose();
            }
        }

        base.Dispose(
            isDisposing: isDisposing);
    }

    #endregion Protected Methods

    #region Private Methods

    private void InitializeCells(IContainerProvider containerProvider, Grid grid)
    {
        var rowsCount = 0;
        var columnsCount = 0;

        if (grid?.Cells?.Count > 0)
        {
            var isOnCanvas = parent == default
                && grid.Cells.Count == 1;

            foreach (var element in grid.Cells)
            {
                switch (element)
                {
                    case Player player:
                        {
                            var playerCell = new PlayerViewModel(
                                containerProvider: containerProvider,
                                player: player,
                                parent: this);

                            Cells.Add(playerCell);

                            rowsCount = Math.Max(rowsCount, element.Row + 1);
                            columnsCount = Math.Max(columnsCount, element.Column + 1);

                            break;
                        }

                    case Group group:
                        {
                            var groupCell = new GroupViewModel(
                                containerProvider: containerProvider,
                                group: group,
                                parent: this,
                                isOnCanvas: isOnCanvas);

                            Cells.Add(groupCell);

                            rowsCount = Math.Max(rowsCount, groupCell.Row + groupCell.Rows);
                            columnsCount = Math.Max(columnsCount, groupCell.Column + groupCell.Columns);

                            break;
                        }
                }
            }
        }

        Rows = rowsCount;
        Columns = columnsCount;

        base.Initialize();
    }

    private void ShowLayout()
    {
        eventAggregator?.GetEvent<TabVisibleEvent>().Publish(grid);

        panelService?.Activate<IGridService>();
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
