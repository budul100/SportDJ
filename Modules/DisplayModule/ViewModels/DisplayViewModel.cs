using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using System;

namespace SportDJ.Modules.DisplayModule.ViewModels
{
    public class DisplayViewModel
        : BindableBase, IDisposable
    {
        #region Private Fields

        private readonly DispatcherTimer clockTimer;
        private readonly IPlayerService playerService;

        private string content;
        private bool isDisposed;
        private string remaining;

        #endregion Private Fields

        #region Public Constructors

        public DisplayViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            playerService = containerProvider?.Resolve<IPlayerService>(
                name: nameof(PlayerType.Background));

            clockTimer = GetClockTimer();
            clockTimer.Start();

            eventAggregator?.GetEvent<PlayerStartedEvent>().Subscribe(
                action: s => OnPlayerStarted(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<PlayerUpdatedEvent>().Subscribe(
                action: s => OnPlayerUpdated(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<PlayerStoppedEvent>().Subscribe(
                action: s => OnPlayerEnded(s),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public static string Clock => DateTime.Now.ToString(Constants.FormatClock);

        public string Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public string Remaining
        {
            get { return remaining; }
            set { SetProperty(ref remaining, value); }
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

        private void OnPlayerEnded(IPlayerService playerService)
        {
            if (playerService == this.playerService)
            {
                Content = default;
                Remaining = default;
            }
        }

        private void OnPlayerStarted(IPlayerService playerService)
        {
            if (playerService == this.playerService
                && playerService.Track != default)
            {
                Content = $"{playerService.Track.Artist} - {playerService.Track.Title}";
            }
        }

        private void OnPlayerUpdated(IPlayerService playerService)
        {
            if (playerService == this.playerService
                && playerService.Track != default)
            {
                Remaining = playerService.Remaining.ToString(Constants.FormatDuration);
            }
        }

        #endregion Private Methods
    }
}