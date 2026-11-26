using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
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

    private readonly IControllerService controllerService;
    private readonly IDialogService dialogService;
    private readonly IDispatcherService dispatcherService;
    private readonly IEventAggregator eventAggregator;
    private readonly IKeyboardService keyListenService;
    private readonly Lock lockKeys = new();
    private readonly HashSet<Key> registeredKeys = [];
    private readonly IStorageService<Set> settingsService;

    private Grid listeningGrid;
    private Grid listeningMidiGrid;
    private CancellationTokenSource midiSource;

    #endregion Private Fields

    #region Public Constructors

    public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
        IDialogService dialogService, IKeyboardService keyListenService, IDispatcherService dispatcherService,
        IControllerService controllerService)
    {
        this.controllerService = controllerService;
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
            var added = default(Grid);

            // The new tab is announced after the grid update, so its view model exists when it is shown
            if (Apply(() => added = grid.Group.Add(
                anchor: grid,
                orientation: orientation)))
            {
                eventAggregator.GetEvent<TabAddedEvent>().Publish(added);
            }
        }
    }

    public void AddPlayer(Cell cell, OrientationType orientation)
    {
        if (cell?.Grid != default)
        {
            var added = default(Cell);

            // The new player is announced after the grid update, so its view model exists when it is shown
            if (Apply(() => added = cell.Grid.Add(
                anchor: cell,
                orientation: orientation))
                && added is Player player)
            {
                eventAggregator.GetEvent<PlayerAddedEvent>().Publish(player);
            }
        }
    }

    public void CancelListening()
    {
        CancelMidi();

        var grid = listeningGrid;

        if (grid != default)
        {
            listeningGrid = default;

            keyListenService.End(grid);

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
        }
    }

    public void ClearMidi(Grid grid)
    {
        SetKey(
            grid: grid,
            key: default);
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

    public bool IsListeningMidi(Grid grid)
    {
        return grid != default
            && ReferenceEquals(listeningMidiGrid, grid);
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

    public void ListenMidi(Grid grid)
    {
        if (grid != default)
        {
            _ = ListenMidiAsync(grid);
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

    public void MoveIntoGroup(Grid grid, Group group)
    {
        // Not into its own group and not into a group, which is part of the tab itself
        if (grid?.Group != default
            && group != default
            && grid.Group != group
            && !grid.Contains(group))
        {
            if (Apply(() => grid.MoveToGroup(group)))
            {
                eventAggregator.GetEvent<TabMovedEvent>().Publish(grid);
            }
        }
    }

    public void MoveIntoGroup(Player player, Group group)
    {
        // A player, which is the only cell of its grid, cannot leave it, the grid would be empty
        if (player?.Grid?.Cells?.Count > 1
            && group != default)
        {
            var moved = default(Grid);

            if (Apply(() => moved = player.MoveToGroup(group)))
            {
                eventAggregator.GetEvent<TabMovedEvent>().Publish(moved);
            }
        }
    }

    public void MoveOutOfGroup(Grid grid)
    {
        if (grid?.Group?.Grids?.Count > 1)
        {
            if (Apply(grid.MoveOut))
            {
                eventAggregator.GetEvent<TabMovedEvent>().Publish(grid);
            }
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
        // The last tab of a group cannot be removed, the group would be empty
        if (grid?.Group?.Grids?.Count > 1)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.TabRemoveMessage,
                contentTitle: Texts.TabRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                Apply(grid.Remove);
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

            // Invalid input does not notify, as the UI would write the stored name back while the user is typing.
            // The view restores the stored name, when the input loses the focus.
            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
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
    // if it throws or if it grows the grid beyond MaxCount. Returns true if the change was kept.
    private bool Apply(Action change)
    {
        if (Grid == default)
        {
            return false;
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

        var isKept = !IsLimitExceeded(sizeBefore, Grid.Measure());

        if (isKept)
        {
            Persist();
        }
        else
        {
            snapshot.Restore();
        }

        RegisterKeys();

        CancelListening();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();

        return isKept;
    }

    private void CancelMidi()
    {
        try
        {
            midiSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The listener has already finished
        }
    }

    private void Initialize()
    {
        Grid = settingsService.Contents.Grid
            ?? new Grid();

        Grid.Tidy();

        Grid.Initialize();

        RegisterKeys();

        Persist();

        CancelListening();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    private async Task ListenMidiAsync(Grid grid)
    {
        // Replaces a pending keyboard or MIDI assignment
        CancelListening();

        var source = new CancellationTokenSource();

        midiSource = source;
        listeningMidiGrid = grid;

        eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);

        try
        {
            var key = await controllerService.ListenAsync(
                cancellationToken: source.Token);

            SetKey(
                grid: grid,
                key: key);
        }
        catch (OperationCanceledException)
        {
            // Listening was replaced by another assignment, cancelled or the controller was closed
        }
        finally
        {
            // A replaced listener must not reset the state of its successor
            if (ReferenceEquals(midiSource, source))
            {
                midiSource = default;
                listeningMidiGrid = default;
            }

            source.Dispose();

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
        }
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

    // Registers the MIDI keys of all tabs anew, as tabs are added, removed or get another key
    private void RegisterKeys()
    {
        lock (lockKeys)
        {
            foreach (var registered in registeredKeys)
            {
                controllerService.Unregister(registered);
            }

            registeredKeys.Clear();

            foreach (var tab in Grid.GetTabs())
            {
                if (tab.Key != default
                    && registeredKeys.Add(tab.Key))
                {
                    var current = tab;

                    controllerService.Register(
                        key: tab.Key,
                        strokeType: StrokeType.Down,
                        action: () => eventAggregator.GetEvent<TabKeyPressedEvent>().Publish(current));
                }
            }
        }
    }

    private void SetKey(Grid grid, Key key)
    {
        if (grid != default
            && grid.Key != key)
        {
            grid.Key = key;

            Persist();

            RegisterKeys();

            eventAggregator.GetEvent<TabUpdatedEvent>().Publish(grid);
        }
    }

    #endregion Private Methods
}
