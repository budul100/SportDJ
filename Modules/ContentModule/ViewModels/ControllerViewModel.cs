using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Registers;
using SportDJ.Commons.Events.Signals;
using SportDJ.Commons.Events.Tracks;
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
        private readonly IRegisterService registerService;
        private string artist;
        private Controller controller;
        private string counter;
        private IPlayerService playerService;
        private Position position;
        private string remaining;
        private ISignalService signalService;
        private string title;
        private ITrackService trackService;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IRegisterService registerService)
        {
            this.containerProvider = containerProvider;
            this.registerService = registerService;

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

            eventAggregator?.GetEvent<TrackSelectedEvent>().Subscribe(
                action: (s) => OnTrackUpdated(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<TracksUpdatedEvent>().Subscribe(
                action: (s) => OnTrackUpdated(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<RegisterUpdatedEvent>().Subscribe(
                action: () => UpdateCount(),
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

            trackService = containerProvider?.Resolve<ITrackService>();
            playerService = containerProvider?.Resolve<IPlayerService>(controller.PlayerType.ToString());

            trackService?.Initialize(controller.Path);

            if (this.controller.SavePositions)
            {
                position = GetPosition();

                var track = trackService.Tracks
                    .SingleOrDefault(t => t.Path == position.Path);

                trackService.Select(track);
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
                trackService.Next();
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

        private void OnTrackUpdated(ITrackService trackService)
        {
            if (trackService == this.trackService)
            {
                UpdateTrack();
            }
        }

        private void UpdateCount()
        {
            if (trackService.Track != default)
            {
                var count = registerService.GetCount(trackService.Track);
                Counter = $"[{count}]";
            }
        }

        private void UpdateSignal()
        {
            if (trackService.Track != default)
            {
                signalService = playerService.Add(trackService.Track);

                UpdateTrack();
            }
        }

        private void UpdateTrack()
        {
            if (trackService.Track != default)
            {
                Title = trackService.Track.Title;
                Artist = trackService.Track.Artist;
                Remaining = trackService.Track.Length.ToString(DurationFormat);

                UpdateCount();
            }
        }

        #endregion Private Methods
    }
}