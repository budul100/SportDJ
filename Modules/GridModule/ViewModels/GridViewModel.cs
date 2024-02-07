using Material.Icons;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Models.Structure;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GridViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly GroupViewModel parent;

        private int columns;
        private bool isDisposed;
        private bool isRenaming;
        private bool isSelected;
        private string keyCode;
        private string name;

        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
            : base(containerProvider: containerProvider, cell: grid.Group)
        {
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

        public bool HasKey => !string.IsNullOrWhiteSpace(Key);

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

        public string Key => KeyCode.GetKey();

        public string KeyCode
        {
            get { return keyCode; }
            set
            {
                if (value != keyCode)
                {
                    keyCode = value;

                    RaisePropertyChanged(nameof(Key));
                    RaisePropertyChanged(nameof(HasKey));
                }
            }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

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

        protected override IEnumerable<BindableBase> GetMenuItems()
        {
            var menuCells = GetMenuCells().ToArray();

            var editCellsItem = new MenuViewModel(
                header: $"{Texts.GridEditHeader}...",
                menuItems: menuCells);

            editCellsItem.MenuCloseEvent += OnMenuClosing;

            yield return editCellsItem;

            yield return new SeparatorViewModel();

            var renameItem = new MenuViewModel(
                header: Texts.PlayerRenameHeader,
                callback: () => IsRenaming = true,
                icon: MaterialIconKind.FormTextbox);

            renameItem.MenuCloseEvent += OnMenuClosing;

            yield return renameItem;
        }

        #endregion Protected Methods

        #region Private Methods

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