using Material.Icons;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GridViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly Grid grid;
        private readonly GroupViewModel parent;

        private int columns;
        private bool isDisposed;
        private bool isRenaming;
        private bool isSelected;
        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
            : base(containerProvider: containerProvider, element: grid)
        {
            this.grid = grid;
            this.parent = parent;

            Initialize(
                containerProvider: containerProvider,
                grid: grid);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<CellViewModel> Cells { get; private set; }

        public int Columns
        {
            get { return columns; }
            set { SetProperty(ref columns, value); }
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set { SetProperty(ref isRenaming, value); }
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

        #endregion Public Properties

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing
                    && Cells?.Any() == true)
                {
                    foreach (var cell in Cells)
                    {
                        cell.Dispose();
                    }
                }

                isDisposed = true;
            }

            base.Dispose(
                isDisposing: isDisposing);
        }

        protected override IEnumerable<BindableBase> GetElementMenus()
        {
            var gridMenus = base.GetGridMenus().ToArray();

            var gridItems = new MenuViewModel(
                header: $"{Texts.CellEditHeader}...",
                menuItems: gridMenus,
                closeCallback: CloseMenu);

            yield return gridItems;

            var tabMenus = GetTabMenus().ToArray();

            var tabItems = new MenuViewModel(
                header: $"{Texts.TabEditHeader}...",
                menuItems: tabMenus,
                closeCallback: CloseMenu);

            yield return tabItems;

            var renameItem = new MenuViewModel(
                header: Texts.TabRenameHeader,
                executeCallback: () => IsRenaming = true,
                closeCallback: CloseMenu,
                icon: MaterialIconKind.FormTextbox);

            yield return renameItem;
        }

        #endregion Protected Methods

        #region Private Methods

        private IEnumerable<BindableBase> GetTabMenus()
        {
            var orientations = Enum.GetValues<OrientationType>()
                .Where(o => o == OrientationType.Left || o == OrientationType.Right).ToArray();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.TabAddHeader} {orientation}";

                void callback() => gridService.AddGrid(
                    grid: grid,
                    orientation: orientation);

                var addItem = new MenuViewModel(
                    header: header,
                    executeCallback: callback,
                    closeCallback: CloseMenu);

                yield return addItem;
            }

            yield return new SeparatorViewModel();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.TabMoveHeader} {orientation}";

                void callback() => gridService.MoveGrid(
                    grid: grid,
                    orientation: orientation);

                var moveItem = new MenuViewModel(
                    header: header,
                    executeCallback: callback,
                    closeCallback: CloseMenu);

                yield return moveItem;
            }

            yield return new SeparatorViewModel();

            var removeItem = new MenuViewModel(
                header: $"{Texts.TabRemoveHeader}...",
                executeCallback: () => gridService.RemoveGridAsync(grid),
                closeCallback: CloseMenu);

            yield return removeItem;
        }

        private void Initialize(IContainerProvider containerProvider, Grid grid)
        {
            Cells = new ObservableCollection<CellViewModel>();

            Name = grid.Name;
            KeyCode = grid.KeyCode;

            if (grid?.Cells?.Any() == true)
            {
                var rowsCount = 0;
                var columnsCount = 0;

                foreach (var element in grid.Cells)
                {
                    switch (element)
                    {
                        case Player player:

                            var playerCell = new PlayerViewModel(
                                containerProvider: containerProvider,
                                player: player,
                                parent: this);

                            Cells.Add(playerCell);

                            rowsCount = element.Row + 1 > rowsCount
                                ? element.Row + 1
                                : rowsCount;

                            columnsCount = element.Column + 1 > columnsCount
                                ? element.Column + 1
                                : columnsCount;

                            break;

                        case Group group:

                            var isOnCanvas = parent == default
                                && grid.Cells.Count == 1;

                            var groupCell = new GroupViewModel(
                                containerProvider: containerProvider,
                                group: group,
                                parent: this,
                                isOnCanvas: isOnCanvas);

                            Cells.Add(groupCell);

                            rowsCount = groupCell.Row + groupCell.Rows > rowsCount
                                ? groupCell.Row + groupCell.Rows
                                : rowsCount;

                            columnsCount = groupCell.Column + groupCell.Columns > columnsCount
                                ? groupCell.Column + groupCell.Columns
                                : columnsCount;

                            break;
                    }
                }

                Rows = rowsCount;
                Columns = columnsCount;

                base.Initialize();
            }
        }

        #endregion Private Methods
    }
}