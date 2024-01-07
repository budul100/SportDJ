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
            Rows = 0;
            Columns = 0;

            if (containerProvider != default
                && group.Grids?.Any() == true)
            {
                foreach (var grid in group.Grids)
                {
                    var gridTab = containerProvider.Resolve<GridViewModel>();

                    gridTab.Initialize(grid);

                    Tabs.Add(gridTab);
                }

                Rows = Tabs.Max(t => t.Rows);
                Columns = Tabs.Max(t => t.Columns);

                foreach (var tab in Tabs)
                {
                    tab.Rows = Rows;
                    tab.Columns = Columns;
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
    }
}