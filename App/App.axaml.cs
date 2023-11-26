using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SportDJ.App.Views;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Modules.ContentModule.Views;
using System;
using System.Linq;

namespace SportDJ.App
{
    public class App
        : PrismApplication
    {
        #region Public Properties

        public static bool IsSingleViewLifetime => Environment
            .GetCommandLineArgs().Any(a => a == "--fbdev" || a == "--drm");

        #endregion Public Properties

        #region Public Methods

        public static AppBuilder BuildAvaloniaApp() => AppBuilder
            .Configure<App>()
            .UsePlatformDetect();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            base.Initialize();              // <-- Required
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<Modules.ContentModule.Module>();
        }

        protected override AvaloniaObject CreateShell()
        {
            //if (IsSingleViewLifetime)
            //{
            //    return Container.Resolve<MainControl>(); // For Linux Framebuffer or DRM
            //}
            //else
            //{
            //    return Container.Resolve<MainView>();
            //}

            return Container.Resolve<MainView>();
        }

        protected override void OnInitialized()
        {
            var regionManager = Container.Resolve<IRegionManager>();

            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.ContentRegion),
                viewType: typeof(RootView));
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IContentService, Services.ContentService.Service>();

            containerRegistry.Register<MainView>();
        }

        #endregion Protected Methods
    }
}