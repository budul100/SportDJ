using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using SportDJ.App.Views;
using SportDJ.Commons.Assets;
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

        public static bool IsSingleViewLifetime => Environment.GetCommandLineArgs()
            .Any(a => a == "--fbdev" || a == "--drm");

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
            moduleCatalog.AddModule<Modules.GridModule.Module>(
                name: nameof(Modules.GridModule));
            moduleCatalog.AddModule<Modules.DisplayModule.Module>(
                name: nameof(Modules.DisplayModule));
            moduleCatalog.AddModule<Modules.ControllerModule.Module>(
                name: nameof(Modules.ControllerModule));
            moduleCatalog.AddModule<Modules.SettingsModule.Module>(
                name: nameof(Modules.SettingsModule));
        }

        protected override AvaloniaObject CreateShell()
        {
            LoadSettings();

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
            var dialogService = new DialogService.Service(App.Current);
            containerRegistry.RegisterInstance<IDialogService>(dialogService);

            containerRegistry.RegisterSingleton<IDispatcherService, Services.DispatcherService.Service>();
            containerRegistry.RegisterSingleton<IHookService, Services.HookService.Service>();

            containerRegistry.RegisterSingleton<IStorageService<Set>, Services.StorageService.Service<Set>>();
            containerRegistry.RegisterSingleton<IStorageService<Session>, Services.StorageService.Service<Session>>();
            containerRegistry.RegisterSingleton<ITrackService, Services.TrackService.Service>();

            RegisterTypes<OutputType>(
                containerRegistry: containerRegistry,
                from: typeof(IOutputService),
                to: typeof(Services.OutputService.Service));

            RegisterTypes<OutputType>(
                containerRegistry: containerRegistry,
                from: typeof(IVolumeService),
                to: typeof(Services.VolumeService.Service));

            RegisterTypes<MixerType>(
                containerRegistry: containerRegistry,
                from: typeof(IMixerService),
                to: typeof(Services.MixerService.Service));

            containerRegistry.Register<ISignalService, Services.SignalService.Service>();
            containerRegistry.Register<IStackService, Services.StackService.Service>();

            containerRegistry.Register<MainView>();
        }

        #endregion Protected Methods

        #region Private Methods

        private static void RegisterTypes<TEnum>(IContainerRegistry containerRegistry, Type from, Type to)
            where TEnum : struct, Enum
        {
            var names = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>().Select(t => t.ToString()).ToArray();

            foreach (var name in names)
            {
                containerRegistry.RegisterSingleton(
                    from: from,
                    to: to,
                    name: name);
            }
        }

        private void LoadSettings()
        {
            var settingsService = Container.Resolve<IStorageService<Set>>();

            var commandLinePath = Environment.GetCommandLineArgs().Length > 1
                ? Environment.GetCommandLineArgs()[1]
                : default;

            var settingsPath = commandLinePath?.StartsWith("--") == false
                ? commandLinePath
                : settingsService.GetPath(
                    appName: Constants.AppName,
                    fileName: Defaults.FileNameSettings);

            settingsService.Load(settingsPath);
        }

        #endregion Private Methods
    }
}