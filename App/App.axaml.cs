using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using SportDJ.App.ViewModels;
using SportDJ.App.Views;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.App
{
    public class App
        : PrismApplication
    {
        #region Private Fields

        private IClassicDesktopStyleApplicationLifetime desktop;
        private MainView mainWindow;
        private SplashView splashWindow;

        #endregion Private Fields

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

            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

            if (desktop != default)
            {
                desktop.Startup += OnStartup;

                if (!Debugger.IsAttached)
                {
                    splashWindow = Container.Resolve<SplashView>();

                    desktop.MainWindow = splashWindow;
                    splashWindow.Show();
                }

                var dispatcherService = Container.Resolve<IDispatcherService>();
                Task.Run(() => dispatcherService.InvokeAsync(InitializeApp));
            }

            base.OnFrameworkInitializationCompleted();
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
            moduleCatalog.AddModule<Modules.SearchModule.Module>(
                name: nameof(Modules.SearchModule));
            moduleCatalog.AddModule<Modules.MenuModule.Module>(
                name: nameof(Modules.MenuModule));
        }

        protected override AvaloniaObject CreateShell()
        {
            mainWindow = !IsSingleViewLifetime
                ? Container.Resolve<MainView>()
                : default; // For Linux Framebuffer or DRM: Container.Resolve<MainControl>();

            return mainWindow;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IDialogService, Services.DialogService.Service>();
            containerRegistry.RegisterSingleton<IDispatcherService, Services.DispatcherService.Service>();
            containerRegistry.RegisterSingleton<IStorageService<Set>, Services.StorageService.Service<Set>>();
            containerRegistry.RegisterSingleton<IStorageService<Session>, Services.StorageService.Service<Session>>();
            containerRegistry.RegisterSingleton<ITrackService, Services.TrackService.Service>();
            containerRegistry.RegisterSingleton<IMidiService, Services.MidiService.Service>();
            containerRegistry.RegisterSingleton<IGridService, Services.GridService.Service>();

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

            containerRegistry.Register<IPlayerService, Services.PlayerService.Service>();
            containerRegistry.Register<ISignalService, Services.SignalService.Service>();
            containerRegistry.Register<IStackService, Services.StackService.Service>();

            containerRegistry.RegisterSingleton<MainViewModel>();
        }

        #endregion Protected Methods

        #region Private Methods

        private static void RegisterTypes<TEnum>(IContainerRegistry containerRegistry, Type from, Type to)
            where TEnum : struct, Enum
        {
            var names = Enum.GetValues<TEnum>()
                .Cast<TEnum>().Select(t => t.ToString()).ToArray();

            foreach (var name in names)
            {
                containerRegistry.RegisterSingleton(
                    from: from,
                    to: to,
                    name: name);
            }
        }

        private void InitializeApp()
        {
            try
            {
                var commandLinePath = Environment.GetCommandLineArgs().Length > 1
                ? Environment.GetCommandLineArgs()[1]
                : default;

                var settingsPath = commandLinePath?.StartsWith("--") == false
                    ? commandLinePath
                    : default;

                var mainViewModel = Container.Resolve<MainViewModel>();
                mainViewModel.Initialize(settingsPath);

                desktop.MainWindow = mainWindow;

                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                var iconUri = $"avares://{assemblyName}/Assets/{assemblyName}.png";

                var dialogService = Container.Resolve<IDialogService>();
                dialogService.Initialize(
                    window: mainWindow,
                    iconUri: iconUri);

                splashWindow?.Close();

                var eventAggregator = Container.Resolve<IEventAggregator>();
                eventAggregator.GetEvent<AppInitializedEvent>().Publish();
            }
            catch (TaskCanceledException)
            {
                desktop.MainWindow = default;

                splashWindow?.Close();
            }
        }

        private void OnStartup(object s, ControlledApplicationLifetimeStartupEventArgs e)
        {
            Control.GotFocusEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
            Control.DoubleTappedEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
        }

        #endregion Private Methods
    }
}