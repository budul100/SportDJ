using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.SearchModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.PanelRegion),
                viewType: typeof(Views.SearchView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.SearchView>(
                name: typeof(Views.SearchView).FullName);
        }

        #endregion Public Methods
    }
}