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
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
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
        private readonly IPanelService panelService;
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private bool isDisposed;
        private Control panelBottom;
        private Control panelLeft;
        private Control panelRight;
        private Control panelTop;
        private string title;
        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService, IStorageService<Session> sessionService,
            IDispatcherService dispatcherService, IDialogService dialogService, IPanelService panelService)
        {
            this.containerProvider = containerProvider;
            this.dispatcherService = dispatcherService;
            this.settingsService = settingsService;
            this.sessionService = sessionService;
            this.dialogService = dialogService;
            this.panelService = panelService;

            assemblyTitle = GetAssemblyTitle();

            OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosing);

            ClosePanelTopCommand = new DelegateCommand(() => ClosePanel(PanelType.Top));
            ClosePanelBottomCommand = new DelegateCommand(() => ClosePanel(PanelType.Bottom));
            ClosePanelLeftCommand = new DelegateCommand(() => ClosePanel(PanelType.Left));
            ClosePanelRightCommand = new DelegateCommand(() => ClosePanel(PanelType.Right));

            backgroundMixer = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => InitializeSession(),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
                action: UpdateLayout,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
                action: UpdatePanel,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
                action: _ => UpdateTitle(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: m => backgroundMixer == m);

            eventAggregator?.GetEvent<MixerStoppedEvent>().Subscribe(
                action: _ => UpdateTitle(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: m => backgroundMixer == m);

            LoadSessionService();
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClosePanelBottomCommand { get; }

        public DelegateCommand ClosePanelLeftCommand { get; }

        public DelegateCommand ClosePanelRightCommand { get; }

        public DelegateCommand ClosePanelTopCommand { get; }

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

        public Control PanelBottom
        {
            get => panelBottom;
            private set => SetProperty(ref panelBottom, value);
        }

        public double PanelBottomHeight
        {
            get
            {
                return panelService.IsVisible(PanelType.Bottom)
                    ? sessionService.Contents.PanelHeightBottom
                    : 0;
            }
            set
            {
                if (panelService.IsVisible(PanelType.Bottom)
                    && value != PanelBottomHeight)
                {
                    sessionService.Contents.PanelHeightBottom = value > 0
                        ? value
                        : Defaults.PanelHeight;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(PanelBottomHeight));
                }
            }
        }

        public Control PanelLeft
        {
            get => panelLeft;
            private set => SetProperty(ref panelLeft, value);
        }

        public double PanelLeftWidth
        {
            get
            {
                return panelService.IsVisible(PanelType.Left)
                    ? sessionService.Contents.PanelWidthLeft
                    : 0;
            }
            set
            {
                if (panelService.IsVisible(PanelType.Left)
                    && value != PanelLeftWidth)
                {
                    sessionService.Contents.PanelWidthLeft = value > 0
                        ? value
                        : Defaults.PanelWidth;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(PanelLeftWidth));
                }
            }
        }

        public Control PanelRight
        {
            get => panelRight;
            private set => SetProperty(ref panelRight, value);
        }

        public double PanelRightWidth
        {
            get
            {
                return panelService.IsVisible(PanelType.Right)
                    ? sessionService.Contents.PanelWidthRight
                    : 0;
            }
            set
            {
                if (panelService.IsVisible(PanelType.Right)
                    && value != PanelRightWidth)
                {
                    sessionService.Contents.PanelWidthRight = value > 0
                        ? value
                        : Defaults.PanelWidth;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(PanelRightWidth));
                }
            }
        }

        public Control PanelTop
        {
            get => panelTop;
            private set => SetProperty(ref panelTop, value);
        }

        public double PanelTopHeight
        {
            get
            {
                return panelService.IsVisible(PanelType.Top)
                    ? sessionService.Contents.PanelHeightTop
                    : 0;
            }
            set
            {
                if (panelService.IsVisible(PanelType.Top)
                    && value != PanelTopHeight)
                {
                    sessionService.Contents.PanelHeightTop = value > 0
                        ? value
                        : Defaults.PanelHeight;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(PanelTopHeight));
                }
            }
        }

        public string Title
        {
            get => title;
            private set => SetProperty(ref title, value);
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

            InitializeSession();
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

                    DisposeOutputs();
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

        private void ClosePanel(PanelType type)
        {
            panelService.Deactivate(type);
        }

        private void DisposeOutputs()
        {
            outputServices.OnEach(s => s.Dispose());
        }

        private void InitializeMixer(OutputType outputType, MixerType mixerType, bool onForeground)
        {
            var outputService = containerProvider.Resolve<IOutputService>(
                name: outputType.ToString());

            if (outputService?.WaveFormat != default)
            {
                var mixerService = containerProvider.Resolve<IMixerService>(
                    name: mixerType.ToString());

                if (mixerService?.Output?.WaveFormat != outputService.WaveFormat)
                {
                    mixerService.Initialize(
                        type: mixerType,
                        waveFormat: outputService.WaveFormat,
                        signalsCanOverlap: onForeground);

                    outputService.Add(
                        mixerService: mixerService,
                        onForeground: onForeground);
                }
            }
        }

        private void InitializeOutput(OutputType outputType)
        {
            var outputService = containerProvider?.Resolve<IOutputService>(
                name: outputType.ToString());

            outputService?.Initialize(
                type: outputType);

            outputServices.Add(outputService);

            if (outputType != OutputType.PFL)
            {
                liveServices.Add(outputService);
            }
        }

        private void InitializeSession()
        {
            InitializeOutput(
                outputType: OutputType.Live);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Background,
                onForeground: false);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Foreground,
                onForeground: true);

            //InitializeOutput(
            //    type: OutputType.PFL);

            //InitializeMixer(
            //    outputType: OutputType.PFL,
            //    mixerType: MixerType.PFL,
            //    onForeground: true);

            UpdateLayout();
            UpdateTitle();
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

            RaisePropertyChanged(nameof(PanelTopHeight));
            RaisePropertyChanged(nameof(PanelBottomHeight));
            RaisePropertyChanged(nameof(PanelLeftWidth));
            RaisePropertyChanged(nameof(PanelRightWidth));
        }

        private void UpdatePanel(PanelType region)
        {
            switch (region)
            {
                case PanelType.Top:
                    PanelTop = panelService.GetView(region);
                    RaisePropertyChanged(nameof(PanelTopHeight));
                    break;

                case PanelType.Bottom:
                    PanelBottom = panelService.GetView(region);
                    RaisePropertyChanged(nameof(PanelBottomHeight));
                    break;

                case PanelType.Left:
                    PanelLeft = panelService.GetView(region);
                    RaisePropertyChanged(nameof(PanelLeftWidth));
                    break;

                case PanelType.Right:
                    PanelRight = panelService.GetView(region);
                    RaisePropertyChanged(nameof(PanelRightWidth));
                    break;
            }
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