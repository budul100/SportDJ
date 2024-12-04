using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.MenuModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            //regionManager.RegisterViewWithRegion(
            //    regionName: nameof(RegionType.MenuRegion),
            //    viewType: typeof(Views.MenuView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.MenuView>(
                name: nameof(RegionType.MenuRegion));
        }

        #endregion Public Methods
    }
}