using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Settings;
using SportDJ.Modules.SettingsModule.ViewModels;
using SportDJ.Modules.SettingsModule.Views;

namespace SportDJ.Modules.SettingsModule;

public static class Module
{
    #region Public Methods

    public static void Initialize(IContainerProvider containerProvider)
    {
        // The view model reacts to events from the start, so it is created now.
        // The view (property grid) is expensive and only needed once the panel is opened.
        containerProvider.Resolve<SettingsViewModel>();

        var panelService = containerProvider.Resolve<IPanelService>();

        panelService.Register<IStorageService<Session>>(
            type: PanelType.Right,
            viewFactory: () => containerProvider.Resolve<SettingsView>());
    }

    #endregion Public Methods
}
