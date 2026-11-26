using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SportDJ.App.Ioc;
using SportDJ.App.ViewModels;
using SportDJ.App.Views;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.OutputService;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.ViewModelLocator;

namespace SportDJ.App;

public class App
    : Application
{
    #region Private Fields

    private readonly Stopwatch startupWatch = new();

    private IContainerProvider container;
    private IClassicDesktopStyleApplicationLifetime desktop;

    private MainView mainWindow;

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
        startupWatch.Start();

        AvaloniaXamlLoader.Load(this);

        // The previewer only needs the styles, not the module graph
        if (Design.IsDesignMode)
        {
            return;
        }

        LogStartup("XAML loaded");

        container = CreateContainer();

        LogStartup("Container built");

        // Views get their view models from the container, so this has to be set before the first view is created
        ViewModelLocator.Resolver = type => container.Resolve(type);

        mainWindow = !IsSingleViewLifetime
            ? container.Resolve<MainView>()
            : default; // For Linux Framebuffer or DRM: container.Resolve<MainControl>();

        LogStartup("MainView created");

        Modules.DefinitionsModule.Module.Initialize(container);
        Modules.SettingsModule.Module.Initialize(container);

        LogStartup("Modules initialized");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (desktop != default
            && container != default)
        {
            desktop.Startup += OnStartup;

            var dispatcherService = container.Resolve<IDispatcherService>();
            Task.Run(() => dispatcherService.InvokeAsync(InitializeApp));
        }

        base.OnFrameworkInitializationCompleted();
    }

    #endregion Public Methods

    #region Private Methods

    private static IContainerProvider CreateContainer()
    {
        var registry = new ContainerRegistry();

        registry.RegisterSingleton<IEventAggregator, EventAggregator>();

        registry.RegisterSingleton<MainViewModel>();

        registry.RegisterSingleton<IDialogService, Services.DialogService.Service>();
        registry.RegisterSingleton<IDispatcherService, Services.DispatcherService.Service>();
        registry.RegisterSingleton<IPanelService, Services.PanelService.Service>();
        registry.RegisterSingleton<IStorageService<Set>, Services.StorageService.Service<Set>>();
        registry.RegisterSingleton<IStorageService<Session>, Services.StorageService.Service<Session>>();
        registry.RegisterSingleton<ITrackService, Services.TrackService.Service>();
        registry.RegisterSingleton<IGridService, Services.GridService.Service>();
        registry.RegisterSingleton<IControllerService, Services.ControllerService.Service>();
        registry.RegisterSingleton<IKeyboardService, Services.KeyboardService.Service>();

        registry.Register<IPlayerService, Services.PlayerService.Service>();
        registry.Register<IStackService, Services.StackService.Service>();

        RegisterTypes<MixerType>(
            registry: registry,
            from: typeof(IMixerService),
            to: typeof(Services.MixerService.Service));

        RegisterTypes<OutputType>(
            registry: registry,
            from: typeof(IOutputService),
            to: typeof(Service));

        registry.Register<IDeviceService, Services.DeviceService.Windows.Service>();
        registry.Register<ISignalService, Services.SignalService.Windows.Service>();

        registry.Register<ISourceProvider, Shareds.SampleProviders.Windows.SourceProvider>();

        // Not named, so all of them are injected as IEnumerable<IMonitorService>
        registry.RegisterSingleton<IMonitorService, Services.MonitorService.Windows.Service>();
        registry.RegisterSingleton<IMonitorService, Services.MonitorService.Mixxx.Service>();

        RegisterDefinitionsModule(registry);

        // One instance, created at startup (see the settings module), the lazily created view picks it up
        registry.RegisterSingleton<Modules.SettingsModule.ViewModels.SettingsViewModel>();

        return registry.Build();
    }

    private static void RegisterDefinitionsModule(ContainerRegistry registry)
    {
        registry.RegisterSingleton<Modules.DefinitionsModule.Views.DefinitionsView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.DefinitionsViewModel>();

        registry.RegisterSingleton<Modules.DefinitionsModule.Views.SettingsView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.SettingsViewModel>();
        registry.RegisterSingleton<Modules.DefinitionsModule.Views.TabSettingsView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.TabSettingsViewModel>();
        registry.RegisterSingleton<Modules.DefinitionsModule.Views.StackView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.StackViewModel>();
        registry.RegisterSingleton<Modules.DefinitionsModule.Views.LayoutView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.LayoutViewModel>();
        registry.RegisterSingleton<Modules.DefinitionsModule.Views.ControllerSettingsView>();
        registry.RegisterSingleton<Modules.DefinitionsModule.ViewModels.ControllerSettingsViewModel>();
    }

    private static void RegisterTypes<TEnum>(ContainerRegistry registry, Type from, Type to)
        where TEnum : struct, Enum
    {
        var names = Enum.GetValues<TEnum>()
            .Cast<TEnum>().Select(t => t.ToString()).ToArray();

        foreach (var name in names)
        {
            registry.RegisterSingleton(
                service: from,
                implementation: to,
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

            var mainViewModel = container.Resolve<MainViewModel>();
            mainViewModel.Initialize(settingsPath);

            LogStartup("MainViewModel initialized");

            desktop.MainWindow = mainWindow;

            // Without Prism nobody shows the shell, so this has to happen explicitly
            mainWindow.Show();

            LogStartup("Main window shown");

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var iconUri = $"avares://{assemblyName}/Assets/{assemblyName}.png";

            var dialogService = container.Resolve<IDialogService>();
            dialogService.Initialize(
                window: mainWindow,
                iconUri: iconUri);

            var eventAggregator = container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<AppInitializedEvent>().Publish();
        }
        catch (TaskCanceledException)
        {
            desktop.MainWindow = default;
        }
        catch (Exception exception)
        {
            // Otherwise the exception disappears silently in the Task.Run and the app just shows nothing
            var capturedException = ExceptionDispatchInfo.Capture(exception);

            Dispatcher.UIThread.Post(() => capturedException.Throw());
        }
    }

    private void LogStartup(string phase)
    {
        // Trace (not Debug) also works in release builds, e.g. with DebugView
        Trace.WriteLine($"[Startup] {startupWatch.ElapsedMilliseconds,6} ms  {phase}");
    }

    private void OnStartup(object s, ControlledApplicationLifetimeStartupEventArgs e)
    {
        Control.GotFocusEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
        Control.DoubleTappedEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
    }

    #endregion Private Methods
}