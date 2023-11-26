using Prism.Ioc;
using SportDJ.Commons.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class GroupViewModel
        : BaseViewModel
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
            Initialize(group.Name);

            if (group.Contents?.Any() == true)
            {
                var element = containerProvider.Resolve<GridViewModel>();

                element.Initialize(
                    contents: group.Contents,
                    name: group.Name);

                Elements.Add(element);
            }
        }

        #endregion Public Methods
    }
}