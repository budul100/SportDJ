using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Events;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    internal class RootViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IContentService contentService;

        private GridViewModel element;

        #endregion Private Fields

        #region Public Constructors

        public RootViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IContentService contentService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.contentService = contentService;

            eventAggregator.GetEvent<ContentLoadedEvent>().Subscribe(
                action: LoadSet,
                keepSubscriberReferenceAlive: true);

            LoadSet();
        }

        #endregion Public Constructors

        #region Public Properties

        public GridViewModel Element
        {
            get { return element; }
            set { SetProperty(ref element, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void LoadSet()
        {
            var set = contentService.Set;

            Element = containerProvider.Resolve<GridViewModel>();
            Element.Initialize(set.Contents);
        }

        #endregion Private Methods
    }
}