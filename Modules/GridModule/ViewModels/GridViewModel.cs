using Prism.Ioc;
using Prism.Mvvm;
using SharpHook.Native;
using SportDJ.Commons.Models.Structure;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GridViewModel
        : BindableBase
    {
        #region Private Fields

        private int columns;
        private KeyCode? key;
        private string name;
        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider, Grid grid, bool isOnCanvas)
        {
            Name = grid.Name;
            Key = grid.Key;

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
                                player: player);

                            Cells.Add(playerCell);

                            rowsCount = element.Row + 1 > rowsCount
                                ? element.Row + 1
                                : rowsCount;

                            columnsCount = element.Column + 1 > columnsCount
                                ? element.Column + 1
                                : columnsCount;

                            break;

                        case Group group:

                            isOnCanvas = isOnCanvas
                                && grid.Cells.Count == 1;

                            var groupCell = new GroupViewModel(
                                containerProvider: containerProvider,
                                group: group,
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
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();

        public int Columns
        {
            get { return columns; }
            set { SetProperty(ref columns, value); }
        }

        public bool HasKey => Key.HasValue;

        public KeyCode? Key
        {
            get { return key; }
            set
            {
                SetProperty(ref key, value);

                RaisePropertyChanged(nameof(HasKey));
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
    }
}