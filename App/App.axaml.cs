using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using SportDJ.App.Views;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
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
            moduleCatalog.AddModule<Modules.DisplayModule.Module>(
                name: nameof(Modules.DisplayModule));
            moduleCatalog.AddModule<Modules.ContentModule.Module>(
                name: nameof(Modules.ContentModule));
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

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IDispatcherService, Services.DispatcherService.Service>();
            containerRegistry.RegisterSingleton<ITrackService, Services.TrackService.Service>();
            containerRegistry.RegisterSingleton<IStorageService<Set>, Services.StorageService.Service<Set>>();
            containerRegistry.RegisterSingleton<IStorageService<Session>, Services.StorageService.Service<Session>>();

            containerRegistry.RegisterSingleton<IOutputService, Services.OutputService.Service>(
                name: nameof(OutputType.Live));
            containerRegistry.RegisterSingleton<IOutputService, Services.OutputService.Service>(
                name: nameof(OutputType.PFL));

            containerRegistry.RegisterSingleton<IPlayerService, Services.PlayerService.Service>(
                name: nameof(PlayerType.Music));
            containerRegistry.RegisterSingleton<IPlayerService, Services.PlayerService.Service>(
                name: nameof(PlayerType.Effects));
            containerRegistry.RegisterSingleton<IPlayerService, Services.PlayerService.Service>(
                name: Constants.PlayerNamePFL);

            containerRegistry.Register<ISignalService, Services.SignalService.Service>();
            containerRegistry.Register<IStackService, Services.StackService.Service>();

            containerRegistry.Register<MainView>();
        }

        #endregion Protected Methods
    }
}