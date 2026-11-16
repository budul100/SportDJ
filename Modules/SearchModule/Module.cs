using Prism.Ioc;
using Prism.Modularity;

namespace SportDJ.Modules.SearchModule;

public class Module()
    : IModule
{
    #region Public Methods

    public void OnInitialized(IContainerProvider containerProvider)
    { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<Views.SearchView>(
            name: typeof(Views.SearchView).FullName);
    }

    #endregion Public Methods
}