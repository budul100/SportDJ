using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SportDJ.Commons.Enums;

namespace SportDJ.Modules.SettingsModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.SettingsRegion),
                viewType: typeof(Views.SettingsView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.SettingsView>(
                name: typeof(Views.SettingsView).FullName);
        }

        #endregion Public Methods
    }
}