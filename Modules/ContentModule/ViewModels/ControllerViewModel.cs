using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Events.Signals;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ControllerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private Controller controller;
        private IPlayerService playerService;
        private Position position;
        private ISignalService signalService;
        private ITrackService trackService;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            this.containerProvider = containerProvider;

            ExecuteSignalCommand = new DelegateCommand(ExecuteSignal);

            eventAggregator.GetEvent<SignalStoppedEvent>().Subscribe(
                action: (s) => OnSignalStopped(s),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ExecuteSignalCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Controller controller)
        {
            base.Initialize(
                cell: controller);

            this.controller = controller;

            trackService = containerProvider.Resolve<ITrackService>();
            InitializeTracks(controller);

            if (trackService.Tracks.Any())
            {
                playerService = containerProvider.Resolve<IPlayerService>(controller.PlayerType.ToString());

                SetSignal();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void ExecuteSignal()
        {
            if (signalService != default)
            {
                if (signalService.IsPlaying)
                {
                    signalService.Pause();
                }
                else
                {
                    signalService.Start();
                }
            }
        }

        private Position GetPosition(Controller controller)
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

        private void InitializeTracks(Controller controller)
        {
            trackService.Initialize(controller.Path);

            if (this.controller.SavePositions)
            {
                position = GetPosition(controller);

                var track = trackService.Tracks
                    .SingleOrDefault(t => t.Path == position.Path);

                trackService.Select(track);
            }
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                signalService.Dispose();
                trackService.Next();

                SetSignal();
            }
        }

        private void SetSignal()
        {
            if (trackService.Track != default)
            {
                signalService = playerService.Add(trackService.Track);
            }
        }

        #endregion Private Methods
    }
}