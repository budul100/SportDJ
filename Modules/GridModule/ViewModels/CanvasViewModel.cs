using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.GridModule.ViewModels
{
    internal class CanvasViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IStorageService<Set> settingsService;

        private GridViewModel element;

        #endregion Private Fields

        #region Public Constructors

        public CanvasViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SettingsLoadedEvent>().Subscribe(
                action: Initialize,
                keepSubscriberReferenceAlive: true);

            Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public GridViewModel Grid
        {
            get { return element; }
            set { SetProperty(ref element, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void Initialize()
        {
            if (containerProvider != default)
            {
                Grid = containerProvider.Resolve<GridViewModel>();

                Grid.Initialize(settingsService.Contents.Grid);
            }
        }

        #endregion Private Methods
    }
}