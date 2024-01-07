using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Models.Structure;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.PlayerModule.ViewModels
{
    public class GridViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private string name;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();

        public int Columns { get; private set; }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public int Rows { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Grid grid)
        {
            Name = grid.Name;

            var rows = 0;
            var columns = 0;

            if (containerProvider != default
                && grid?.Cells?.Any() == true)
            {
                foreach (var element in grid.Cells)
                {
                    switch (element)
                    {
                        case Player player:

                            var playerCell = containerProvider.Resolve<PlayerViewModel>();
                            playerCell.Initialize(player);

                            Cells.Add(playerCell);

                            rows = element.Row + 1 > rows
                                ? element.Row + 1
                                : rows;

                            columns = element.Column + 1 > columns
                                ? element.Column + 1
                                : columns;

                            break;

                        case Group group:

                            var groupCell = containerProvider.Resolve<GroupViewModel>();
                            groupCell.Initialize(group);

                            Cells.Add(groupCell);

                            rows = groupCell.Row + groupCell.Rows > rows
                                ? groupCell.Row + groupCell.Rows
                                : rows;

                            columns = groupCell.Column + groupCell.Columns > columns
                                ? groupCell.Column + groupCell.Columns
                                : columns;

                            break;
                    }
                }
            }

            Rows = rows;
            RaisePropertyChanged(nameof(Rows));

            Columns = columns;
            RaisePropertyChanged(nameof(Columns));
        }

        #endregion Public Methods
    }
}