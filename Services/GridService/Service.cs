using System.Collections.Generic;
using System.Linq;
using MsBox.Avalonia.Enums;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using SportDJ.Services.GridService.Extensions;

namespace SportDJ.Services.GridService
{
    public class Service
        : IGridService
    {
        #region Private Fields

        private readonly IDialogService dialogService;
        private readonly Dictionary<Cell, Grid> elements = new();
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

            eventAggregator.GetEvent<SettingsLoadedEvent>().Subscribe(
                action: InitializeGrid,
                keepSubscriberReferenceAlive: true);

            InitializeGrid();
        }

        #endregion Public Constructors

        #region Public Properties

        public Grid Grid { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Add<T>(Cell cell, OrientationType orientation)
            where T : Cell
        {
            var column = cell.GetColumn(orientation);
            var row = cell.GetRow(orientation);

            elements[cell].Move(
                orientation: orientation,
                column: column,
                row: row);

            if (typeof(T) == typeof(Player))
            {
                AddPlayer(
                    grid: elements[cell],
                    column: column,
                    row: row);
            }

            UpdateGrid();
        }

        public void Move(Cell cell, OrientationType orientation)
        {
            elements[cell].Move(
                orientation: orientation,
                column: cell.Column,
                row: cell.Row);

            UpdateGrid();
        }

        public async void RemoveAsync(Cell cell)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Constants.GridRemoveMessage,
                contentTitle: Constants.GridRemoveHeader);

            if (result == ButtonResult.Yes)
            {
                RemoveElement(cell);

                UpdateGrid();
            }
        }

        public async void SwapAsync<T>(T cell)
            where T : Cell
        {
            if (cell is Player player)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Constants.GridSwapPlayerMessage,
                    contentTitle: Constants.GridSwapHeader);

                if (result == ButtonResult.Yes)
                {
                    SwapPlayer(player);

                    UpdateGrid();
                }
            }
            else if (cell is Group group)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Constants.GridSwapGroupMessage,
                    contentTitle: Constants.GridSwapHeader);

                if (result == ButtonResult.Yes)
                {
                    SwapGroup(group);

                    UpdateGrid();
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddPlayer(Grid grid, int column = 0, int row = 0)
        {
            var player = grid.GetPlayer();

            player.Column = column;
            player.Row = row;

            elements.Add(
                key: player,
                value: grid);
        }

        private void InitializeGrid()
        {
            Grid = settingsService.Contents.Grid
                ?? new Grid();

            Grid.Tidy();

            elements.Clear();

            UpdateElements(Grid);

            UpdateGrid();
        }

        private void RemoveElement(Cell cell)
        {
            var grid = elements[cell];

            grid.Cells.Remove(cell);

            elements.Remove(cell);
        }

        private void SwapGroup(Group group)
        {
            var givenGrid = elements[group];

            if (group?.Grids.Any() == true)
            {
                var cells = group.Grids
                    .SelectMany(g => g.Cells)
                    .Where(c => c != default).ToArray();

                foreach (var cell in cells)
                {
                    cell.Column = group.Column;
                    cell.Row = group.Row;

                    givenGrid.Cells.Add(cell);
                    givenGrid.Cells.Remove(group);

                    elements[cell] = givenGrid;
                }
            }
        }

        private void SwapPlayer(Player player)
        {
            var givenGrid = elements[player];

            var group = new Group
            {
                Column = player.Column,
                Row = player.Row,
            };

            givenGrid.Cells.Add(group);

            elements.Add(
                key: group,
                value: givenGrid);

            var name = group.GetTabName();

            var newGrid = new Grid
            {
                Name = name,
            };

            group.Grids.Add(newGrid);

            newGrid.Cells.Add(player);
            givenGrid.Cells.Remove(player);

            elements[player] = newGrid;
        }

        private void UpdateElements(Grid grid)
        {
            if (grid?.Cells?.Any() == true)
            {
                foreach (var cell in grid.Cells)
                {
                    elements.Add(
                        key: cell,
                        value: grid);

                    if (cell is Group group)
                    {
                        foreach (var groupGrid in group.Grids)
                        {
                            UpdateElements(groupGrid);
                        }
                    }
                }
            }
        }

        private void UpdateGrid()
        {
            Grid.Tidy();

            settingsService.Contents.Grid = Grid;

            settingsService.Save();

            eventAggregator.GetEvent<GridUpdatedEvent>().Publish();
        }

        #endregion Private Methods
    }
}