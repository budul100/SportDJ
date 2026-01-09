using System.Collections.ObjectModel;
using Prism.Navigation.Regions;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.SearchModule.ViewModels
{
    public class SearchViewModel(IRegionManager regionManager)
        : RegionViewModelBase(regionManager)
    {
        #region Public Properties

        public ObservableCollection<Entry> Entries { get; } = [];

        #endregion Public Properties
    }
}