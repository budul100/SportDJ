using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ControllerViewModel
        : CellViewModel
    {
        #region Protected Fields

        protected const string DurationFormat = @"mm\:ss\.f";

        #endregion Protected Fields

        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly ITrackService trackService;
        private string artist;
        private Controller controller;
        private string counter;
        private IPlayerService playerService;
        private Position position;
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

            ExecuteSignalCommand = new DelegateCommand(ExecuteSignal);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: (s) => OnSignalUpdate(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalForwardEvent>().Subscribe(
                action: (s) => OnSignalForward(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: (s) => OnSignalStopped(s),
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

        public DelegateCommand ExecuteSignalCommand { get; }

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

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Controller controller)
        {
            base.Initialize(
                cell: controller);

            this.controller = controller;

            stackService = containerProvider?.Resolve<IStackService>();
            playerService = containerProvider?.Resolve<IPlayerService>(controller.PlayerType.ToString());

            stackService?.Initialize(controller.Path);

            if (this.controller.SavePositions)
            {
                position = GetPosition();

                var track = stackService.Tracks
                    .SingleOrDefault(t => t.Path == position.Path);

                stackService.Select(track);
            }

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

        private Position GetPosition()
        {
            var sessionService = containerProvider.Resolve<IStorageService<Session>>();

            if (!sessionService.Contents.Positions.Any(p => p.Name == controller.Name))
            {
                var position = new Position
                {
                    Name = controller.Name,
                };

                sessionService.Contents.Positions.Add(position);
            }

            var result = sessionService.Contents.Positions
                .First(p => p.Name == controller.Name);

            return result;
        }

        private void OnSignalForward(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                stackService.Next();
            }
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if (signalService == this.signalService
                && (signalService.State == StateType.None || signalService.IsNextThresholdExceeded))
            {
                signalService.Dispose();
                UpdateSignal();
            }
        }

        private void OnSignalUpdate(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                Remaining = signalService.Remaining.ToString(DurationFormat);
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

        private void UpdateSignal()
        {
            if (stackService.Track != default)
            {
                signalService = playerService.Add(stackService.Track);

                UpdateTrack();
            }
        }

        private void UpdateTrack()
        {
            if (stackService.Track != default)
            {
                Title = stackService.Track.Title;
                Artist = stackService.Track.Artist;
                Remaining = stackService.Track.Length.ToString(DurationFormat);

                var count = trackService.GetCount(stackService.Track);
                Counter = $"[{count}]";
            }
        }

        #endregion Private Methods
    }
}