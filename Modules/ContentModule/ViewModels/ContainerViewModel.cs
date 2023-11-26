using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ContainerViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IContentService contentService;

        private BindableBase element;

        #endregion Private Fields

        #region Public Constructors

        public ContainerViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IContentService contentService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.contentService = contentService;
        }

        #endregion Public Constructors

        #region Public Properties

        public BindableBase Element
        {
            get { return element; }
            set { SetProperty(ref element, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Content content)
        {
            switch (content)
            {
                case Player player:
                    Element = containerProvider.Resolve<PlayerViewModel>();
                    (Element as PlayerViewModel).Initialize(player);

                    break;

                case Group group:
                    Element = containerProvider.Resolve<GroupViewModel>();
                    (Element as GroupViewModel).Initialize(group);

                    break;
            }
        }

        #endregion Public Methods
    }
}