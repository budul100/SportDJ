using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DisplayModule.ViewModels;

public class DisplayViewModel
    : BindableBase, IDisposable
{
    #region Private Fields

    private readonly DispatcherTimer clockTimer;
    private readonly IEventAggregator eventAggregator;
    private readonly IKeyboardService keyboardService;
    private readonly IMixerService mixerService;
    private readonly IPanelService panelService;
    private readonly IStorageService<Session> sessionService;
    private readonly IStorageService<Set> settingsService;

    private Bitmap bitmap;
    private string content;
    private bool isDisposed;
    private bool isEnding;
    private bool isPlaying;
    private bool isSettingsLoading;
    private string lastWindowState;
    private double? progress;
    private string remaining;

    #endregion Private Fields

    #region Public Constructors

    public DisplayViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator, IPanelService panelService, IStorageService<Set> settingsService,
        IStorageService<Session> sessionService, IKeyboardService keyboardService)
    {
        this.eventAggregator = eventAggregator;
        this.keyboardService = keyboardService;
        this.panelService = panelService;
        this.settingsService = settingsService;
        this.sessionService = sessionService;

        ShowSettingsCommand = new DelegateCommand(
            executeMethod: ShowSettings);

        mixerService = containerProvider?.Resolve<IMixerService>(
            name: nameof(MixerType.Base));

        eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
            action: _ => OnMixerUpdated(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: m => mixerService == m);

        eventAggregator?.GetEvent<MixerStoppedEvent>().Subscribe(
            action: _ => OnMixerStopped(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: m => mixerService == m);

        eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
            action: () => RaisePropertyChanged(nameof(IsSettingsVisible)),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: t => RaisePropertyChanged(nameof(IsSettingsVisible)),
            keepSubscriberReferenceAlive: true,
            threadOption: ThreadOption.UIThread,
            filter: p => p.Type == typeof(IStorageService<Session>));

        clockTimer = GetClockTimer();
        clockTimer.Start();
    }

    #endregion Public Constructors

    #region Public Properties

    // An instance property, because the change is raised by the instance
    public static string Clock => DateTime.Now.ToString(Constants.FormatClock);

    public Bitmap Bitmap
    {
        get { return bitmap; }
        set { SetProperty(ref bitmap, value); }
    }

    public string Content
    {
        get { return content; }
        set { SetProperty(ref content, value); }
    }

    public bool IsEnding
    {
        get { return isEnding; }
        set { SetProperty(ref isEnding, value); }
    }

    public bool IsPlaying
    {
        get { return isPlaying; }
        set { SetProperty(ref isPlaying, value); }
    }

    // The settings are about to open, building their view can take a moment
    public bool IsSettingsLoading
    {
        get { return isSettingsLoading; }
        set { SetProperty(ref isSettingsLoading, value); }
    }

    public bool IsSettingsVisible => panelService.IsVisible<IStorageService<Session>>()
        && settingsService?.Contents?.Options != default;

    public double? Progress
    {
        get { return progress; }
        set { SetProperty(ref progress, value); }
    }

    public string Remaining
    {
        get { return remaining; }
        set { SetProperty(ref remaining, value); }
    }

    public bool ShowFullscreen
    {
        get
        {
            return IsFullScreen();
        }
        set
        {
            if (IsFullScreen() != value)
            {
                if (value)
                {
                    lastWindowState = sessionService.Contents.WindowState;
                    sessionService.Contents.WindowState = nameof(WindowState.FullScreen);
                }
                else if (!string.IsNullOrWhiteSpace(lastWindowState)
                    && lastWindowState != nameof(WindowState.FullScreen)
                    && lastWindowState != nameof(WindowState.Minimized))
                {
                    sessionService.Contents.WindowState = lastWindowState;
                }
                else
                {
                    sessionService.Contents.WindowState = nameof(WindowState.Normal);
                }

                RaisePropertyChanged(nameof(ShowFullscreen));

                eventAggregator.GetEvent<SettingsUpdatedEvent<Session>>().Publish();
            }
        }
    }

    public DelegateCommand ShowSettingsCommand { get; }

    #endregion Public Properties

    #region Public Methods

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                clockTimer.Stop();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void ActivateSettings()
    {
        try
        {
            panelService.Activate<IStorageService<Session>>();
        }
        finally
        {
            IsSettingsLoading = false;
        }
    }

    private DispatcherTimer GetClockTimer()
    {
        var result = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        result.Tick += (s, e) => RaisePropertyChanged(nameof(ViewModels.DisplayViewModel.Clock));

        return result;
    }

    private bool IsFullScreen()
    {
        // TryParse, so an invalid value in the session file cannot throw
        var result = Enum.TryParse<WindowState>(sessionService?.Contents?.WindowState, out var state)
            && state == WindowState.FullScreen;

        return result;
    }

    private void OnKeyPressed(string keyCode)
    {
        // While anything waits for a key to be assigned, the key must not switch the screen
        if (keyboardService?.IsAny != true
            && keyCode == settingsService?.Contents?.Options?.KeyCodeScreenChange)
        {
            ShowFullscreen = !ShowFullscreen;
        }
    }

    private void OnMixerStopped()
    {
        UpdateDisplay();
    }

    private void OnMixerUpdated()
    {
        UpdateDisplay();
    }

    private void ShowSettings()
    {
        if (IsSettingsLoading)
        {
            return;
        }

        if (IsSettingsVisible || settingsService?.Contents?.Options == default)
        {
            panelService.Deactivate<IStorageService<Session>>();
        }
        else
        {
            // The pending frame shows the loading state first, then the (possibly slow) activation follows
            IsSettingsLoading = true;

            Dispatcher.UIThread.Post(
                action: ActivateSettings,
                priority: DispatcherPriority.Background);
        }
    }

    private void UpdateDisplay()
    {
        if (mixerService.Track != default
            && mixerService.IsPlaying)
        {
            Content = mixerService.Track?.Info;
            IsPlaying = mixerService.IsPlaying;
            IsEnding = mixerService.IsEnding;

            Bitmap = mixerService.Track?.Bitmap;
            Remaining = mixerService.Remaining?.ToString(Constants.FormatDuration);
            Progress = mixerService.Progress;
        }
        else
        {
            Content = default;
            IsPlaying = false;
            IsEnding = false;

            Bitmap = default;
            Progress = default;
            Remaining = default;
        }
    }

    #endregion Private Methods
}
