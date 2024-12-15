using Prism.Navigation.Regions;
using SportDJ.Commons.Prism;
using System.Collections.ObjectModel;

namespace SportDJ.Modules.SearchModule.ViewModels
{
    public class SearchViewModel(IRegionManager regionManager)
        : RegionViewModelBase(regionManager)
    {
        #region Public Properties

        public ObservableCollection<TrackViewModel> Tracks { get; } = [];

        #endregion Public Properties
    }
}