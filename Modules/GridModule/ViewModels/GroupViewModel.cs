using Prism.Ioc;
using SportDJ.Commons.Models.Structure;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GroupViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private GridViewModel tab;

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

        public GridViewModel Tab
        {
            get { return tab; }
            set
            {
                SetCellVisibility(value);
                tab = value;
            }
        }

        public ObservableCollection<GridViewModel> Tabs { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Group group, bool isOnCanvas)
        {
            Rows = 1;
            Columns = 1;

            if (containerProvider != default
                && group.Grids?.Any() == true)
            {
                foreach (var grid in group.Grids)
                {
                    var gridTab = containerProvider.Resolve<GridViewModel>();

                    gridTab.Initialize(grid, false);

                    Tabs.Add(gridTab);
                }

                if (!isOnCanvas)
                {
                    Rows = Tabs.Max(t => t.Rows);
                    Columns = Tabs.Max(t => t.Columns);

                    foreach (var tab in Tabs)
                    {
                        tab.Rows = Rows;
                        tab.Columns = Columns;
                    }
                }
            }

            RaisePropertyChanged(nameof(Rows));
            RaisePropertyChanged(nameof(Columns));

            base.Initialize(
                cell: group,
                rowSpan: Rows,
                columnSpan: Columns);
        }

        #endregion Public Methods

        #region Private Methods

        private void SetCellVisibility(GridViewModel value)
        {
            foreach (var tab in Tabs)
            {
                var players = tab.Cells
                    .OfType<PlayerViewModel>().ToArray();

                foreach (var player in players)
                {
                    player.IsVisible = tab == value;
                }
            }
        }

        #endregion Private Methods
    }
}