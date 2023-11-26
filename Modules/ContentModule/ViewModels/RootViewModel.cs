using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Events;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    internal class RootViewModel : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IContentService contentService;

        private ContainerViewModel element;

        #endregion Private Fields

        #region Public Constructors

        public RootViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IContentService contentService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.contentService = contentService;

            eventAggregator.GetEvent<ContentLoadedEvent>().Subscribe(
                action: LoadContent,
                keepSubscriberReferenceAlive: true);

            LoadContent();
        }

        #endregion Public Constructors

        #region Public Properties

        public ContainerViewModel Element
        {
            get { return element; }
            set { SetProperty(ref element, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void LoadContent()
        {
            var content = contentService.GetContent();

            Element = containerProvider.Resolve<ContainerViewModel>();
            Element.Initialize(content);
        }

        #endregion Private Methods
    }
}