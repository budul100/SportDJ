using System.Collections.ObjectModel;
using System.Linq;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GroupViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly GridViewModel parent;

        private GridViewModel selected;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider, Group group, GridViewModel parent, bool isOnCanvas)
            : base(group)
        {
            this.parent = parent;

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: k => SelectTab(k),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: _ => parent.IsVisible);

            Initialize(
                containerProvider: containerProvider,
                group: group,
                isOnCanvas: isOnCanvas);
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns { get; private set; }

        public bool IsVisible => parent.IsVisible;

        public int Rows { get; private set; }

        public GridViewModel Selected
        {
            get { return selected; }
            set
            {
                if (value != default
                    && value != selected)
                {
                    SetProperty(ref selected, value);

                    foreach (var tab in Tabs)
                    {
                        tab.IsSelected = tab == value;
                    }
                }
            }
        }

        public ObservableCollection<GridViewModel> Tabs { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Private Methods

        private void Initialize(IContainerProvider containerProvider, Group group, bool isOnCanvas)
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
                        parent: this);

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

        private void SelectTab(string keyCode)
        {
            if (IsVisible)
            {
                var selection = Tabs
                    .FirstOrDefault(t => t.KeyCode == keyCode);

                Selected = selection;
            }
        }

        #endregion Private Methods
    }
}