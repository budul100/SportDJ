using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
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
            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.DisplayRegion),
                viewType: typeof(Views.DisplayView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.DisplayView>(
                name: typeof(Views.DisplayView).FullName);
        }

        #endregion Public Methods
    }
}