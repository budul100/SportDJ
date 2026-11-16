using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Prism;
using System.Collections.ObjectModel;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class CanvasViewModel
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

            // The collection is bound to the UI, so it must only be changed on the UI thread
            eventAggregator.GetEvent<GridUpdatedEvent>().Subscribe(
                action: Initialize,
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true);

            // The grid might already be available, if the event was published before this view model exists
            if (gridService.Grid != default)
            {
                Initialize();
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<GridViewModel> Grids { get; } = [];

        #endregion Public Properties

        #region Private Methods

        private void Initialize()
        {
            // The old grid has to leave the collection, otherwise all grids are stacked on each other
            Grids.Clear();

            grid?.Dispose();
            grid = default;

            if (gridService.Grid != default)
            {
                grid = new GridViewModel(
                    containerProvider: containerProvider,
                    grid: gridService.Grid);

                Grids.Add(grid);
            }
        }

        #endregion Private Methods
    }
}