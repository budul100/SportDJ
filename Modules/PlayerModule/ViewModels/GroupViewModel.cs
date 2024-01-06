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

        public ObservableCollection<GridViewModel> Elements { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Group group)
        {
            if (group.Contents?.Any() == true
                && containerProvider != default)
            {
                var element = containerProvider.Resolve<GridViewModel>();

                element.Initialize(group);

                Elements.Add(element);
            }

            base.Initialize(
                cell: group,
                rowSpan: group.Rows,
                columnSpan: group.Columns);
        }

        #endregion Public Methods
    }
}