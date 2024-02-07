using System.Collections.ObjectModel;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Prism;

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

            eventAggregator.GetEvent<GridUpdatedEvent>().Subscribe(
                action: Initialize,
                keepSubscriberReferenceAlive: true);

            Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<GridViewModel> Grids { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Private Methods

        private void Initialize()
        {
            grid?.Dispose();

            grid = new GridViewModel(
                containerProvider: containerProvider,
                grid: gridService.Grid);

            Grids.Add(grid);
        }

        #endregion Private Methods
    }
}