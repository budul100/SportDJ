using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ControllerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly ITrackService trackService;

        private string artist;
        private string counter;
        private IPlayerService playerService;
        private string remaining;
        private ISignalService signalService;
        private IStackService stackService;
        private string title;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            ITrackService trackService)
        {
            this.containerProvider = containerProvider;
            this.trackService = trackService;

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecuteSignal,
                canExecuteMethod: () => IsExecuteAvailable());

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => UpdateStack(true),
                canExecuteMethod: () => IsNextTrackAvailable());

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => UpdateStack(false),
                canExecuteMethod: () => IsNextTrackAvailable());

            eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
                action: (s) => OnSignalStarted(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: (s) => OnSignalStopped(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: (s) => OnSignalUpdate(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: (s) => OnSignalNext(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: (s) => OnStackUpdated(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: (t) => OnTrackUpdated(t),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public string Artist
        {
            get { return artist; }
            set { SetProperty(ref artist, value); }
        }

        public string Counter
        {
            get { return counter; }
            set { SetProperty(ref counter, value); }
        }

        public DelegateCommand ExecuteCommand { get; }

        public bool HasStack => stackService?.Entries?.Count > 1;

        public bool IsActive => signalService?.State == StateType.Play || signalService?.IsSilent == false;

        public string Remaining
        {
            get { return remaining; }
            set { SetProperty(ref remaining, value); }
        }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Controller controller)
        {
            base.Initialize(
                cell: controller);

            stackService = containerProvider?.Resolve<IStackService>();
            playerService = containerProvider?.Resolve<IPlayerService>(controller.PlayerType.ToString());

            stackService?.Initialize(
                path: controller.Path,
                controllerName: controller.Name,
                savePosition: controller.SavePositions);

            UpdateSignal();
        }

        #endregion Public Methods

        #region Private Methods

        private void ExecuteSignal()
        {
            if (signalService != default)
            {
                if (signalService.State == StateType.Play)
                {
                    signalService.Pause();
                }
                else
                {
                    signalService.Start();
                }
            }
        }

        private bool IsExecuteAvailable()
        {
            var result = stackService?.Track != default;

            return result;
        }

        private bool IsNextTrackAvailable()
        {
            var result = stackService?.HasUnplayeds == true;

            return result;
        }

        private void OnSignalNext(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                UpdateStack();
            }
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                if (this.signalService.Track == this.stackService?.Track)
                {
                    this.stackService.Play();
                }

                UpdateCommands();
            }
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                if (signalService.State == StateType.None || signalService.IsNextThresholdExceeded)
                {
                    signalService.Dispose();

                    UpdateStack();
                }

                UpdateCommands();
            }
        }

        private void OnSignalUpdate(ISignalService signalService)
        {
            if (signalService == this.signalService
                && this.signalService.Track == this.stackService?.Track)
            {
                Remaining = signalService.Remaining.ToString(Constants.FormatDuration);
            }
        }

        private void OnStackUpdated(IStackService stackService)
        {
            if (stackService == this.stackService)
            {
                UpdateTrack();
            }
        }

        private void OnTrackUpdated(Track track)
        {
            if (track == this.stackService?.Track)
            {
                UpdateTrack();
            }
        }

        private void UpdateCommands()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(HasStack));

            ExecuteCommand.RaiseCanExecuteChanged();
            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateSignal()
        {
            if (stackService.Track != default)
            {
                signalService = playerService.Add(stackService.Track);

                UpdateTrack();
            }
        }

        private void UpdateStack(bool toPrevious = false)
        {
            if (toPrevious)
            {
                stackService.Previous();
            }
            else
            {
                stackService.Next();
            }

            UpdateSignal();
        }

        private void UpdateTrack()
        {
            if (stackService.Track != default)
            {
                Title = stackService.Track.Title;
                Artist = stackService.Track.Artist;
                Remaining = stackService.Track.Length.ToString(Constants.FormatDuration);

                var count = trackService.GetCount(stackService.Track);
                Counter = $"[{count}]";
            }

            UpdateCommands();
        }

        #endregion Private Methods
    }
}