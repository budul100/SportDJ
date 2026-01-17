using Prism.Ioc;
using Prism.Modularity;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Modules.StackModule.Views;

namespace SportDJ.Modules.StackModule
{
    public class Module()
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var panelService = containerProvider.Resolve<IPanelService>();
            var stackView = containerProvider.Resolve<StackView>();

            panelService.Register<IStackService>(
                type: PanelType.Left,
                view: stackView);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<StackView>();

            containerRegistry.RegisterForNavigation<StackView>(
                name: typeof(StackView).FullName);
        }

        #endregion Public Methods
    }
}