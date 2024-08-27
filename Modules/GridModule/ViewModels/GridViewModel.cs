using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Material.Icons;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GridViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly Grid grid;
        private readonly IHookService hookService;
        private readonly GroupViewModel parent;

        private int columns;
        private bool isDisposed;
        private bool isListening;
        private bool isRenaming;
        private bool isSelected;
        private Action listenChecker;
        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
            : base(containerProvider: containerProvider, element: grid)
        {
            this.grid = grid;
            this.parent = parent;

            hookService = containerProvider?.Resolve<IHookService>();

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
            set
            {
                if (IsRenaming != value)
                {
                    SetProperty(ref isRenaming, value);

                    if (value)
                    {
                        hookService.Deactivate(this);
                    }
                    else
                    {
                        hookService.Activate(this);
                    }
                }
            }
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

            var gridMenu = new MenuViewModel(
                header: $"{Texts.GridEditHeader}...",
                menuItems: gridMenus,
                closeCallback: CloseMenu);

            yield return gridMenu;

            var groupMenus = GetGroupMenus().ToArray();

            var groupMenu = new MenuViewModel(
                header: $"{Texts.GroupEditHeader}...",
                menuItems: groupMenus,
                closeCallback: CloseMenu);

            yield return groupMenu;

            var tabMenus = GetTabMenus().ToArray();

            var tabMenu = new MenuViewModel(
                header: $"{Texts.TabEditHeader}...",
                menuItems: tabMenus,
                closeCallback: CloseMenu);

            yield return tabMenu;
        }

        #endregion Protected Methods

        #region Private Methods

        private IEnumerable<BindableBase> GetGroupMenus()
        {
            var orientations = Enum.GetValues<OrientationType>()
                .Where(o => o == OrientationType.Left || o == OrientationType.Right).ToArray();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.GroupAddHeader} {orientation}";

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
                var header = $"{Texts.GroupMoveHeader} {orientation}";

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

        private IEnumerable<BindableBase> GetTabMenus()
        {
            var renameItem = new MenuViewModel(
                header: Texts.TabRenameHeader,
                executeCallback: () => IsRenaming = true,
                closeCallback: CloseMenu,
                icon: MaterialIconKind.FormTextbox);

            yield return renameItem;

            var listenItem = new MenuViewModel(
                header: Texts.KeyListenHeader,
                executeCallback: () => ListenHook(),
                isCheckedGetter: () => isListening,
                icon: MaterialIconKind.KeyboardOutline);

            listenChecker = () => listenItem.Update();

            yield return listenItem;

            var unhookItem = new MenuViewModel(
                header: Texts.KeyUnhookHeader,
                executeCallback: () => KeyCode = default,
                closeCallback: CloseMenu,
                icon: MaterialIconKind.KeyboardOffOutline);

            yield return unhookItem;
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

        private void ListenHook()
        {
            if (!isListening)
            {
                isListening = true;

                hookService.Listen(k => ReceiveHook(k));
            }
        }

        private void ReceiveHook(string keyCode)
        {
            if (isListening)
            {
                KeyCode = keyCode;
                isListening = false;

                listenChecker.Invoke();

                CloseMenu();
            }
        }

        #endregion Private Methods
    }
}