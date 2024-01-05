using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.DisplayModule
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
                regionName: nameof(RegionType.DisplayRegion),
                source: typeof(Views.DisplayView).FullName);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.DisplayView>(
                name: typeof(Views.DisplayView).FullName);
        }

        #endregion Public Methods
    }
}