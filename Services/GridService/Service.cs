using System;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using SportDJ.Services.GridService.Extensions;

namespace SportDJ.Services.GridService;

public class Service
    : IGridService
{
    #region Public Fields

    /// <summary>
    /// Maximum number of columns, rows and tabs. Operations that would grow a grid beyond
    /// this limit are ignored and leave the grid unchanged.
    /// </summary>
    public const int MaxCount = 256;

    #endregion Public Fields

    #region Private Fields

    private readonly IDialogService dialogService;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageService<Set> settingsService;

    #endregion Private Fields

    #region Public Constructors

    public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
        IDialogService dialogService)
    {
        this.eventAggregator = eventAggregator;
        this.settingsService = settingsService;
        this.dialogService = dialogService;

        eventAggregator.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
            action: Initialize,
            keepSubscriberReferenceAlive: true);
    }

    #endregion Public Constructors

    #region Public Properties

    public Grid Grid { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void AddGrid(Grid grid, OrientationType orientation)
    {
        if (grid?.Group != default)
        {
            Apply(() => grid.Group.Add(
                anchor: grid,
                orientation: orientation));
        }
    }

    public void AddPlayer(Cell cell, OrientationType orientation)
    {
        if (cell?.Grid != default)
        {
            Apply(() => cell.Grid.Add(
                anchor: cell,
                orientation: orientation));
        }
    }

    public void MoveCell(Cell cell, OrientationType orientation)
    {
        if (cell != default)
        {
            Apply(() => cell.Move(orientation));
        }
    }

    public void MoveGrid(Grid grid, OrientationType orientation)
    {
        if (grid != default)
        {
            Apply(() => grid.Move(orientation));
        }
    }

    public async Task RemoveCellAsync(Cell cell)
    {
        if (cell?.Grid != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.CellRemoveMessage,
                contentTitle: Texts.CellRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                Apply(() => cell.Grid.Cells.Remove(cell));
            }
        }
    }

    public async Task RemoveGridAsync(Grid grid)
    {
        if (grid?.Group != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.TabRemoveMessage,
                contentTitle: Texts.TabRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                Apply(() => grid.Group.Grids.Remove(grid));
            }
        }
    }

    public async Task SwapAsync<T>(T cell)
        where T : Cell
    {
        if (cell != default)
        {
            if (cell is Player player)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.CellSwapPlayerMessage,
                    contentTitle: Texts.CellSwapHeader);

                if (result == ButtonResult.Yes)
                {
                    Apply(player.ToGroup);
                }
            }
            else if (cell is Group group)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.CellSwapGroupMessage,
                    contentTitle: Texts.CellSwapHeader);

                if (result == ButtonResult.Yes)
                {
                    Apply(group.ToPlayer);
                }
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    // A dimension only counts as exceeded if it is above the limit and has grown,
    // so that already oversized settings stay editable.
    private static bool IsLimitExceeded((int Columns, int Rows, int Tabs) before, (int Columns, int Rows, int Tabs) after)
    {
        return (after.Columns > MaxCount && after.Columns > before.Columns)
            || (after.Rows > MaxCount && after.Rows > before.Rows)
            || (after.Tabs > MaxCount && after.Tabs > before.Tabs);
    }

    // Runs the change, tidies the grid and checks the limits. The change is undone
    // if it throws or if it grows the grid beyond MaxCount.
    private void Apply(Action change)
    {
        if (Grid == default)
        {
            return;
        }

        var sizeBefore = Grid.Measure();
        var snapshot = new Snapshot(Grid);

        try
        {
            change();

            Grid.Tidy();
        }
        catch
        {
            snapshot.Restore();

            throw;
        }

        if (IsLimitExceeded(sizeBefore, Grid.Measure()))
        {
            snapshot.Restore();
        }
        else
        {
            Persist();
        }

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    private void Initialize()
    {
        Grid = settingsService.Contents.Grid
            ?? new Grid();

        Grid.Tidy();

        Grid.Initialize();

        Persist();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    private void Persist()
    {
        settingsService.Contents.Grid = Grid;

        settingsService.Save();
    }

    #endregion Private Methods
}
