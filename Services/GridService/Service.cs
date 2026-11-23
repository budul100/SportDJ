using System;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
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
    private readonly IDispatcherService dispatcherService;
    private readonly IEventAggregator eventAggregator;
    private readonly IKeyboardService keyListenService;
    private readonly IStorageService<Set> settingsService;

    private Grid listeningGrid;

    #endregion Private Fields

    #region Public Constructors

    public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
        IDialogService dialogService, IKeyboardService keyListenService, IDispatcherService dispatcherService)
    {
        this.dispatcherService = dispatcherService;
        this.keyListenService = keyListenService;
        this.eventAggregator = eventAggregator;
        this.settingsService = settingsService;
        this.dialogService = dialogService;

        // Only the tab which waits for an assignment takes the key
        eventAggregator.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => IsListening(listeningGrid));

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

    public void CancelListening()
    {
        var grid = listeningGrid;

        if (grid != default)
        {
            listeningGrid = default;

            keyListenService.End(grid);

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
        }
    }

    public async Task GroupAsync(Player player)
    {
        if (player != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.CellGroupMessage,
                contentTitle: Texts.CellGroupHeader);

            if (result == ButtonResult.Yes)
            {
                Apply(player.ToGroup);
            }
        }
    }

    public bool IsListening(Grid grid)
    {
        return grid != default
            && keyListenService.IsListening(grid);
    }

    public void ListenKey(Grid grid)
    {
        if (grid != default)
        {
            var previous = listeningGrid;

            listeningGrid = grid;

            keyListenService.Begin(grid);

            if (previous != default
                && previous != grid)
            {
                eventAggregator.GetEvent<TabUpdatedEvent>().Publish(previous);
            }

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
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
                Apply(grid.Dissolve);
            }
        }
    }

    public void SetKeyCode(Grid grid, string keyCode)
    {
        if (grid != default
            && grid.KeyCode != keyCode)
        {
            grid.KeyCode = keyCode;

            Persist();

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
        }
    }

    public void SetName(Grid grid, string name)
    {
        var trimmed = name?.Trim();

        if (grid != default
            && !string.IsNullOrWhiteSpace(trimmed)
            && grid.Name != trimmed)
        {
            grid.Name = trimmed;

            Persist();
        }

        // Also notify for invalid input, so the UI falls back to the stored name
        eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
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

        CancelListening();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    private void Initialize()
    {
        Grid = settingsService.Contents.Grid
            ?? new Grid();

        Grid.Tidy();

        Grid.Initialize();

        Persist();

        CancelListening();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    private void OnKeyPressed(string keyCode)
    {
        var grid = listeningGrid;

        SetKeyCode(
            grid: grid,
            keyCode: keyCode);

        // Ends later, so other receivers of this key press still see the listening state
        dispatcherService.Post(CancelListening);
    }

    private void Persist()
    {
        settingsService.Contents.Grid = Grid;

        settingsService.Save();
    }

    #endregion Private Methods
}
