using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class GridViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private int columns;
        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns
        {
            get { return columns; }
            set { SetProperty(ref columns, value); }
        }

        public ObservableCollection<CellViewModel> Elements { get; } = new ObservableCollection<CellViewModel>();

        public int Rows
        {
            get { return rows; }
            set { SetProperty(ref rows, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize<T>(T grid)
            where T : IGrid
        {
            if (grid?.Contents?.Any() == true
                && containerProvider != default)
            {
                Rows = grid.Rows;
                Columns = grid.Columns;

                foreach (var content in grid.Contents)
                {
                    switch (content)
                    {
                        case Controller controller:

                            var controllerElement = containerProvider.Resolve<ControllerViewModel>();
                            controllerElement.Initialize(controller);

                            Elements.Add(controllerElement);

                            break;

                        case Group group:

                            var groupElement = containerProvider.Resolve<GroupViewModel>();
                            groupElement.Initialize(group);

                            Elements.Add(groupElement);

                            break;
                    }
                }
            }
        }

        #endregion Public Methods
    }
}