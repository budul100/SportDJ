using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.ControllerModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.ControllerRegion),
                viewType: typeof(Views.ControllerView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        { }

        #endregion Public Methods
    }
}