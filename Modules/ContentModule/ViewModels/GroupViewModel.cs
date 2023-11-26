using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class GroupViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private string name;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<BindableBase> Elements { get; } = new ObservableCollection<BindableBase>();

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Group group)
        {
            this.Name = group.Name;

            if (group.Contents?.Any() == true)
            {
                foreach (var content in group.Contents)
                {
                    var element = containerProvider.Resolve<ContainerViewModel>();
                    element.Initialize(content);

                    Elements.Add(element);
                }
            }
        }

        #endregion Public Methods
    }
}