using System.Collections.Generic;
using System.Threading.Tasks;
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
            if (cell != default)
            {
                var result = default(T);

                if (typeof(T) == typeof(Player))
                {
                    result = cell.Grid.AddPlayer(
                        column: cell.Column,
                        row: cell.Row) as T;
                }

                result.Move(orientation);

                UpdateGrid();
            }
        }

        public void Move(Cell cell, OrientationType orientation)
        {
            if (cell != default)
            {
                cell.Move(orientation);

                UpdateGrid();
            }
        }

        public async Task RemoveAsync(Cell cell)
        {
            if (cell != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.GridRemoveMessage,
                contentTitle: Texts.GridRemoveHeader);

                if (result == ButtonResult.Yes)
                {
                    cell.Remove();

                    UpdateGrid();
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
                        contentMessage: Texts.GridSwapPlayerMessage,
                        contentTitle: Texts.GridSwapHeader);

                    if (result == ButtonResult.Yes)
                    {
                        player.ToGroup();

                        UpdateGrid();
                    }
                }
                else if (cell is Group group)
                {
                    var result = await dialogService.GetMessageBoxResultAsync(
                        contentMessage: Texts.GridSwapGroupMessage,
                        contentTitle: Texts.GridSwapHeader);

                    if (result == ButtonResult.Yes)
                    {
                        group.ToPlayer();

                        UpdateGrid();
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeGrid()
        {
            Grid = settingsService.Contents.Grid
                ?? new Grid();

            Grid.Tidy();
            Grid.Initialize();

            UpdateGrid();
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