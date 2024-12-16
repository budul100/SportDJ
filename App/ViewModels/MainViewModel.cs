using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase, IDisposable
    {
        #region Private Fields

        private readonly string assemblyTitle;
        private readonly IMixerService backgroundMixer;
        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly HashSet<IOutputService> liveServices = [];
        private readonly HashSet<IOutputService> outputServices = [];
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private bool isDisposed;
        private string title;
        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IDispatcherService dispatcherService, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService, IDialogService dialogService)
        {
            this.containerProvider = containerProvider;
            this.dispatcherService = dispatcherService;
            this.settingsService = settingsService;
            this.sessionService = sessionService;
            this.dialogService = dialogService;

            assemblyTitle = GetAssemblyTitle();

            OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosing);

            backgroundMixer = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
                action: UpdateLayout,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
                action: _ => UpdateTitle(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => backgroundMixer == m);

            eventAggregator?.GetEvent<MixerStoppedEvent>().Subscribe(
                action: _ => UpdateTitle(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => backgroundMixer == m);

            LoadSessionService();
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

        public double SettingsWidth
        {
            get
            {
                return settingsService?.Contents?.Options != default && sessionService.Contents.SettingsVisible
                    ? sessionService.Contents.SettingsWidth
                    : 0;
            }
            set
            {
                if (sessionService.Contents.SettingsVisible
                    && value != SettingsWidth)
                {
                    sessionService.Contents.SettingsWidth = value > 0
                        ? value
                        : Defaults.SettingsWidth;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(SettingsWidth));
                }
            }
        }

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

        public void Initialize(string settingsPath)
        {
            if (string.IsNullOrWhiteSpace(settingsPath))
            {
                settingsPath = sessionService?.Contents?.SettingsPath;
            }

            if (string.IsNullOrWhiteSpace(settingsPath))
            {
                settingsPath = settingsService.GetPath(
                    appName: Texts.AppName,
                    fileName: Defaults.FileNameSettings + Constants.SettingsExtension);
            }

            settingsService.Load(
                filePath: settingsPath);

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

            UpdateLayout();
            UpdateTitle();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    settingsService.Save();
                    sessionService.Save();

                    outputServices.OnEach(s => s.Dispose());
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

            var result = $"{Texts.AppName} ({version})";

            return result;
        }

        private async void CheckClosing(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.AppCloseMessage,
                contentTitle: Texts.AppCloseHeader);

            if (result == ButtonResult.Yes)
            {
                userWantsToQuit = true;
                desktop.Shutdown();
            }
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
                    signalsCanOverlap: onForeground);

                outputService.Add(
                    mixerService: mixerService,
                    onForeground: onForeground);

                var volumeService = containerProvider?.Resolve<IVolumeService>(
                    name: outputType.ToString());

                volumeService?.Initialize();
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

        private void LoadSessionService()
        {
            var sessionPath = settingsService?.GetPath(
                appName: Texts.AppName,
                fileName: Defaults.FileNameSession + Constants.SettingsExtension);

            sessionService?.Load(sessionPath);
        }

        private void OnClosing(CancelEventArgs eventArgs)
        {
            userWantsToQuit = userWantsToQuit || !liveServices.Any(s => s.IsPlaying);

            if (!userWantsToQuit
                && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                eventArgs.Cancel = true;

                dispatcherService.Post(() => CheckClosing(desktop));
            }

            if (!eventArgs.Cancel)
            {
                Dispose();
            }
        }

        private void UpdateLayout()
        {
            RaisePropertyChanged(nameof(Height));
            RaisePropertyChanged(nameof(Width));
            RaisePropertyChanged(nameof(WindowState));

            RaisePropertyChanged(nameof(SettingsWidth));
        }

        private void UpdateTitle()
        {
            var result = new StringBuilder();

            result.Append(assemblyTitle);

            if (backgroundMixer?.IsPlaying == true)
            {
                if (!string.IsNullOrWhiteSpace(backgroundMixer.Track?.Info))
                {
                    if (result.Length > 0)
                    {
                        result.Append(Constants.SplitterTitle);
                    }

                    result.Append(backgroundMixer.Track.Info);
                }

                if (backgroundMixer.Remaining.HasValue)
                {
                    if (result.Length > 0)
                    {
                        result.Append(Constants.SplitterTitle);
                    }

                    result.Append(backgroundMixer.Remaining.Value.ToString(Constants.FormatDuration));
                }
            }

            Title = result.ToString();
        }

        #endregion Private Methods
    }
}