using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.DisplayModule.ViewModels
{
    public class DisplayViewModel
        : RegionViewModelBase, IDisposable
    {
        #region Private Fields

        private readonly DispatcherTimer clockTimer;
        private readonly IEventAggregator eventAggregator;
        private readonly IMixerService mixerService;
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private Bitmap bitmap;
        private string content;
        private bool isDisposed;
        private bool isEnding;
        private bool isPlaying;
        private string lastWindowState;
        private double? progress;
        private string remaining;

        #endregion Private Fields

        #region Public Constructors

        public DisplayViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
            : base(regionManager)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.sessionService = sessionService;

            mixerService = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
                action: _ => OnMixerUpdated(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<MixerStoppedEvent>().Subscribe(
                action: _ => OnMixerStopped(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: k => OnKeyPressed(k),
                keepSubscriberReferenceAlive: true);

            clockTimer = GetClockTimer();
            clockTimer.Start();
        }

        #endregion Public Constructors

        #region Public Properties

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

                    eventAggregator.GetEvent<SettingsUpdatedEvent>().Publish();
                }
            }
        }

        public bool ShowSettings
        {
            get
            {
                return sessionService.Contents.SettingsVisible;
            }
            set
            {
                if (sessionService.Contents.SettingsVisible != value)
                {
                    sessionService.Contents.SettingsVisible = value;

                    sessionService.Save();

                    RaisePropertyChanged(nameof(ShowSettings));

                    eventAggregator.GetEvent<SettingsUpdatedEvent>().Publish();
                }
            }
        }

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

        private DispatcherTimer GetClockTimer()
        {
            var result = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            result.Tick += (s, e) => RaisePropertyChanged(nameof(Clock));

            return result;
        }

        private bool IsFullScreen()
        {
            var result = !string.IsNullOrWhiteSpace(sessionService.Contents.WindowState)
                && Enum.Parse<WindowState>(sessionService.Contents.WindowState) == WindowState.FullScreen;

            return result;
        }

        private void OnKeyPressed(string keyCode)
        {
            if (keyCode == settingsService.Contents.Options.KeyCodeScreenChange)
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

        private void UpdateDisplay()
        {
            if (mixerService.Track != default
                && mixerService.IsPlaying)
            {
                Content = mixerService.Track.Info;
                IsPlaying = mixerService.IsPlaying;
                IsEnding = mixerService.IsEnding;

                Bitmap = mixerService.Track.Bitmap;
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
}