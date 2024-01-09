using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Prism;
using System;

namespace SportDJ.Modules.DisplayModule.ViewModels
{
    public class DisplayViewModel
        : RegionViewModelBase, IDisposable
    {
        #region Private Fields

        private readonly DispatcherTimer clockTimer;
        private readonly IMixerService mixerService;

        private Bitmap bitmap;
        private string content;
        private bool isDisposed;
        private bool isEnding;
        private string remaining;
        private long widthPosition;
        private long widthRemaining;

        #endregion Private Fields

        #region Public Constructors

        public DisplayViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            mixerService = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            clockTimer = GetClockTimer();
            clockTimer.Start();

            eventAggregator?.GetEvent<MixerUpdatedEvent>().Subscribe(
                action: s => OnMixerUpdated(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<MixerStoppedEvent>().Subscribe(
                action: _ => OnMixerStopped(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);
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

        public string Remaining
        {
            get { return remaining; }
            set { SetProperty(ref remaining, value); }
        }

        public long WidthPosition
        {
            get { return widthPosition; }
            set { SetProperty(ref widthPosition, value); }
        }

        public long WidthRemaining
        {
            get { return widthRemaining; }
            set { SetProperty(ref widthRemaining, value); }
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

        private void OnMixerStopped()
        {
            Content = default;

            Remaining = default;
            IsEnding = false;

            Bitmap = default;

            WidthPosition = default;
            WidthRemaining = default;
        }

        private void OnMixerUpdated(IMixerService mixerService)
        {
            if (mixerService.Track != default)
            {
                Content = mixerService.Track.Info;

                Remaining = mixerService.Remaining?.ToString(Constants.FormatDuration);
                IsEnding = mixerService.IsEnding;

                Bitmap = mixerService.Track.Bitmap;

                if (mixerService?.Duration?.Ticks > 0)
                {
                    WidthPosition = mixerService.Position.Value.Ticks;
                    WidthRemaining = mixerService.Remaining.Value.Ticks;
                }
            }
        }

        #endregion Private Methods
    }
}