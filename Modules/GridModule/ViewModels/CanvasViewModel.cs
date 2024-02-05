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
        private readonly IGridService gridService;

        private GridViewModel grid;

        #endregion Private Fields

        #region Public Constructors

        public CanvasViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IGridService gridService)
            : base(regionManager)
        {
            this.containerProvider = containerProvider;
            this.gridService = gridService;

            eventAggregator.GetEvent<GridUpdatedEvent>().Subscribe(
                action: Initialize,
                keepSubscriberReferenceAlive: true);

            Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public GridViewModel Grid
        {
            get { return grid; }
            set { SetProperty(ref grid, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void Initialize()
        {
            Grid?.Dispose();

            Grid = new GridViewModel(
                containerProvider: containerProvider,
                grid: gridService.Grid);
        }

        #endregion Private Methods
    }
}