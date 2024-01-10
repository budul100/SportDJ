using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharpHook;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase, IDisposable
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly HashSet<IOutputService> liveServices = new HashSet<IOutputService>();
        private readonly IMessageBoxService messageBoxService;
        private readonly HashSet<IOutputService> outputServices = new HashSet<IOutputService>();
        private readonly IStorageService<Session> sessionService;

        private string assemblyTitle;
        private IMixerService backgroundMixer;
        private TaskPoolGlobalHook hook;
        private bool isDisposed;
        private string title;

        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService, IStorageService<Session> sessionService,
            IMessageBoxService messageBoxService, TaskPoolGlobalHook hook)
        {
            this.containerProvider = containerProvider;
            this.sessionService = sessionService;
            this.messageBoxService = messageBoxService;
            this.hook = hook;

            assemblyTitle = GetAssemblyTitle();

            OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosingAsync);

            LoadSessionService(
                settingsService: settingsService);

            InitializeOutput(
                type: OutputType.Live);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Background,
                onForeground: false);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Foreground,
                onForeground: true);

            InitializeOutput(
                type: OutputType.PFL);

            InitializeMixer(
                outputType: OutputType.PFL,
                mixerType: MixerType.PFL,
                onForeground: true);

            backgroundMixer = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
                action: _ => UpdateTitle(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => backgroundMixer == m);

            if (hook != default)
            {
                Task.Run(() => hook.Run());
            }

            UpdateTitle();
        }

        #endregion Public Constructors

        #region Public Properties

        public int Height
        {
            get
            {
                return sessionService.Contents.Height;
            }
            set
            {
                if (sessionService.Contents.Height != value)
                {
                    sessionService.Contents.Height = value;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(Height));
                }
            }
        }

        public DelegateCommand<CancelEventArgs> OnClosingCommand { get; }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public int Width
        {
            get
            {
                return sessionService.Contents.Width;
            }
            set
            {
                if (sessionService.Contents.Width != value)
                {
                    sessionService.Contents.Width = value;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(Width));
                }
            }
        }

        public WindowState WindowState
        {
            get
            {
                return Enum.Parse<WindowState>(sessionService.Contents.WindowState);
            }
            set
            {
                if (sessionService.Contents.WindowState != value.ToString())
                {
                    sessionService.Contents.WindowState = value.ToString();

                    sessionService.Save();

                    RaisePropertyChanged(nameof(WindowState));
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    outputServices.OnEach(s => s.Dispose());

                    hook.Dispose();
                    hook = default;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static string GetAssemblyTitle()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();

            var version = new StringBuilder();

            version.Append(assembly.Version.Major);

            version.Append(Constants.SplitterVersion);
            version.Append(assembly.Version.Minor);

            if (version.Length > 0
                && assembly.Version.Build > 0)
            {
                version.Append(Constants.SplitterVersion);
                version.Append(assembly.Version.Build);
            }

            var result = $"{Constants.AppName} ({version})";

            return result;
        }

        private void InitializeMixer(OutputType outputType, MixerType mixerType, bool onForeground)
        {
            var outputService = containerProvider.Resolve<IOutputService>(
                name: outputType.ToString());

            if (outputService?.Format != default)
            {
                var mixerService = containerProvider.Resolve<IMixerService>(
                    name: mixerType.ToString());

                mixerService.Initialize(
                    type: mixerType,
                    waveFormat: outputService.Format,
                    playsMultipleSignals: onForeground);

                outputService.Add(
                    mixerService: mixerService,
                    onForeground: onForeground);
            }
        }

        private void InitializeOutput(OutputType type)
        {
            var outputService = containerProvider?.Resolve<IOutputService>(
                name: type.ToString());

            outputService?.Initialize(
                type: type);

            outputServices.Add(outputService);

            if (type != OutputType.PFL)
            {
                liveServices.Add(outputService);
            }
        }

        private void LoadSessionService(IStorageService<Set> settingsService)
        {
            var sessionPath = settingsService?.GetPath(
                appName: Constants.AppName,
                fileName: Defaults.FileNameSession);

            sessionService?.Load(sessionPath);
        }

        private async void OnClosingAsync(CancelEventArgs eventArgs)
        {
            userWantsToQuit = userWantsToQuit || !liveServices.Any(s => s.IsPlaying);

            if (!userWantsToQuit
                && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                eventArgs.Cancel = true;

                var result = await messageBoxService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the application be closed?",
                    contentTitle: "Close application");

                if (result == ButtonResult.Yes)
                {
                    eventArgs.Cancel = false;

                    userWantsToQuit = true;
                    desktop.Shutdown();
                }
            }

            if (!eventArgs.Cancel)
            {
                Dispose();
            }
        }

        private void UpdateTitle()
        {
            var result = new StringBuilder();

            result.Append(assemblyTitle);

            if (!string.IsNullOrWhiteSpace(backgroundMixer?.Track?.Info))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append(backgroundMixer.Track.Info);
            }

            if (backgroundMixer?.Remaining.HasValue == true)
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append(backgroundMixer.Remaining?.ToString(Constants.FormatDuration));
            }

            Title = result.ToString();
        }

        #endregion Private Methods
    }
}