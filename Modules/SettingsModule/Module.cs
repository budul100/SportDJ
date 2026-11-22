using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Settings;
using SportDJ.Modules.SettingsModule.Views;

namespace SportDJ.Modules.SettingsModule;

public static class Module
{
    #region Public Methods

    public static void Initialize(IContainerProvider containerProvider)
    {
        var panelService = containerProvider.Resolve<IPanelService>();
        var settingsView = containerProvider.Resolve<SettingsView>();

        panelService.Register<IStorageService<Session>>(
            type: PanelType.Right,
            view: settingsView);
    }

    #endregion Public Methods
}
