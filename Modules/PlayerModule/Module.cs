using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.PlayerModule
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
            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.PlayerRegion),
                viewType: typeof(Views.GridView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.GridView>(
                name: typeof(Views.GridView).FullName);
        }

        #endregion Public Methods
    }
}