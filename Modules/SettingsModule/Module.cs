using Prism.Ioc;
using Prism.Modularity;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Modules.SettingsModule.Views;

namespace SportDJ.Modules.SettingsModule
{
    public class Module()
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var panelService = containerProvider.Resolve<IPanelService>();
            var settingsView = containerProvider.Resolve<SettingsView>();

            panelService.Register<IStorageService<Session>>(
                type: PanelType.Right,
                view: settingsView);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<SettingsView>();

            containerRegistry.RegisterForNavigation<SettingsView>(
                name: typeof(SettingsView).FullName);
        }

        #endregion Public Methods
    }
}