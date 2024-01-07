using Prism.Ioc;
using SportDJ.Commons.Models.Structure;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.PlayerModule.ViewModels
{
    public class GroupViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns { get; private set; }

        public int Rows { get; private set; }

        public ObservableCollection<GridViewModel> Tabs { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Group group)
        {
            var rows = 0;
            var columns = 0;

            if (containerProvider != default
                && group.Grids?.Any() == true)
            {
                foreach (var grid in group.Grids)
                {
                    var gridTab = containerProvider.Resolve<GridViewModel>();

                    gridTab.Initialize(grid);

                    Tabs.Add(gridTab);
                }

                rows = Tabs.Max(t => t.Rows);
                columns = Tabs.Max(t => t.Columns);
            }

            Rows = rows;
            RaisePropertyChanged(nameof(Rows));

            Columns = columns;
            RaisePropertyChanged(nameof(Columns));

            base.Initialize(
                cell: group,
                rowSpan: rows,
                columnSpan: columns);
        }

        #endregion Public Methods
    }
}