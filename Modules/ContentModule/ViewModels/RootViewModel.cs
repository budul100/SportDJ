using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    internal class RootViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IStorageService<Set> settingsService;

        private GridViewModel element;

        #endregion Private Fields

        #region Public Constructors

        public RootViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SettingsLoadedEvent>().Subscribe(
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
            if (containerProvider != default)
            {
                Element = containerProvider.Resolve<GridViewModel>();

                var root = settingsService.Contents.Root;
                Element.Initialize(root);
            }
        }

        #endregion Private Methods
    }
}