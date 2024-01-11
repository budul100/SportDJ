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

        private GridViewModel tab;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider, Group group, bool isOnCanvas)
            : base(group)
        {
            Rows = 1;
            Columns = 1;

            if (group.Grids?.Any() == true)
            {
                foreach (var grid in group.Grids)
                {
                    var gridTab = new GridViewModel(
                        containerProvider: containerProvider,
                        grid: grid,
                        isOnCanvas: false);

                    Tabs.Add(gridTab);
                }

                if (!isOnCanvas)
                {
                    Rows = Tabs.Max(t => t.Rows);
                    Columns = Tabs.Max(t => t.Columns);

                    foreach (var currentTab in Tabs)
                    {
                        currentTab.Rows = Rows;
                        currentTab.Columns = Columns;
                    }
                }
            }

            this.RowSpan = Rows;
            this.ColumnSpan = Columns;

            RaisePropertyChanged(nameof(Rows));
            RaisePropertyChanged(nameof(Columns));
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns { get; }

        public int Rows { get; }

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