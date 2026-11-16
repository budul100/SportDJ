using Prism.Ioc;
using Prism.Modularity;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Modules.DefinitionsModule.ViewModels;
using SportDJ.Modules.DefinitionsModule.Views;

namespace SportDJ.Modules.DefinitionsModule;

public class Module()
    : IModule
{
    #region Public Methods

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var definitionsViewModel = containerProvider.Resolve<DefinitionsViewModel>();

        var panelService = containerProvider.Resolve<IPanelService>();
        var definitionsView = containerProvider.Resolve<DefinitionsView>();

        panelService?.Register<IPlayerService>(
            type: PanelType.Left,
            view: definitionsView,
            action: definitionsViewModel.Activate<IPlayerService>);

        panelService?.Register<IStackService>(
            type: PanelType.Left,
            view: definitionsView,
            action: definitionsViewModel.Activate<IStackService>);
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<DefinitionsView>();
        containerRegistry.RegisterSingleton<DefinitionsViewModel>();

        containerRegistry.RegisterSingleton<SettingsView>();
        containerRegistry.RegisterSingleton<SettingsViewModel>();
        containerRegistry.RegisterSingleton<StackView>();
        containerRegistry.RegisterSingleton<StackViewModel>();

        containerRegistry.RegisterForNavigation<DefinitionsView>(
            name: typeof(DefinitionsView).FullName);
    }

    #endregion Public Methods
}