using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Modules.DefinitionsModule.ViewModels;
using SportDJ.Modules.DefinitionsModule.Views;

namespace SportDJ.Modules.DefinitionsModule;

public static class Module
{
    #region Public Methods

    public static void Initialize(IContainerProvider containerProvider)
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

    #endregion Public Methods
}
