using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.ContentModule
{
    public class Module
        : IModule
    {
        #region Private Fields

        private readonly IRegionManager regionManager;

        #endregion Private Fields

        #region Public Constructors

        public Module(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        #endregion Public Constructors

        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RequestNavigate(
                regionName: nameof(RegionType.ContentRegion),
                source: typeof(Views.RootView).FullName);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.RootView>(
                name: typeof(Views.RootView).FullName);
        }

        #endregion Public Methods
    }
}