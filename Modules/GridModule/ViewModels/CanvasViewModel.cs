using System.Collections.ObjectModel;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public class CanvasViewModel
    : BindableBase
{
    #region Private Fields

    private readonly ViewModelCache cache = new();
    private readonly IContainerProvider containerProvider;
    private readonly IGridService gridService;

    private GridViewModel grid;

    #endregion Private Fields

    #region Public Constructors

    public CanvasViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator, IGridService gridService)
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
        var model = gridService.Grid;

        if (grid != default
            && model != default
            && grid.Model == model)
        {
            // Same grid, only its structure changed: players which are still there keep their view model and their files
            cache.BeginUpdate();

            grid.Update();

            cache.Sweep();
        }
        else
        {
            // Another grid (e.g. a loaded file): nothing of the old one is reused
            // The old grid has to leave the collection, otherwise all grids are stacked on each other
            Grids.Clear();

            grid?.Dispose();
            grid = default;

            cache.DisposeAll();

            if (model != default)
            {
                cache.BeginUpdate();

                grid = new GridViewModel(
                    containerProvider: containerProvider,
                    cache: cache,
                    grid: model,
                    parent: default);

                Grids.Add(grid);
            }
        }
    }

    #endregion Private Methods
}
