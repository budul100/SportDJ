using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
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
        grid.Group.Add(
            anchor: grid,
            orientation: orientation);

        Update();
    }

    public void AddPlayer(Cell cell, OrientationType orientation)
    {
        cell.Grid.Add(
            anchor: cell,
            orientation: orientation);

        Update();
    }

    public void MoveCell(Cell cell, OrientationType orientation)
    {
        if (cell != default)
        {
            cell.Move(orientation);

            Update();
        }
    }

    public void MoveGrid(Grid grid, OrientationType orientation)
    {
        if (grid != default)
        {
            grid.Move(orientation);

            Update();
        }
    }

    public async Task RemoveCellAsync(Cell cell)
    {
        if (cell != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.CellRemoveMessage,
                contentTitle: Texts.CellRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                cell.Grid.Cells.Remove(cell);

                Update();
            }
        }
    }

    public async Task RemoveGridAsync(Grid grid)
    {
        if (grid != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.TabRemoveMessage,
                contentTitle: Texts.TabRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                var index = grid.Group.Grids.IndexOf(grid);
                grid.Group.Grids.RemoveAt(index);

                Update();
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
                    player.ToGroup();

                    Update();
                }
            }
            else if (cell is Group group)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.CellSwapGroupMessage,
                    contentTitle: Texts.CellSwapHeader);

                if (result == ButtonResult.Yes)
                {
                    group.ToPlayer();

                    Update();
                }
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void Initialize()
    {
        Grid = settingsService.Contents.Grid
            ?? new Grid();

        Grid.Tidy();

        Grid.Initialize();

        Update();
    }

    private void Update()
    {
        Grid.Tidy();

        settingsService.Contents.Grid = Grid;

        settingsService.Save();

        eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
    }

    #endregion Private Methods
}