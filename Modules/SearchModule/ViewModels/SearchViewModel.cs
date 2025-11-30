using Prism.Navigation.Regions;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;
using System.Collections.ObjectModel;

namespace SportDJ.Modules.SearchModule.ViewModels
{
    public class SearchViewModel(IRegionManager regionManager)
        : RegionViewModelBase(regionManager)
    {
        #region Public Properties

        public ObservableCollection<Track> Tracks { get; } = [];

        #endregion Public Properties
    }
}