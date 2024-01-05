using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Modules.ControllerModule.ViewModels
{
    public class ControllerViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IOutputService outputLiveService;

        private bool othersMuted;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: (s) => OnOutputStarted(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: (s) => OnOutputStopped(s),
                keepSubscriberReferenceAlive: true);

            outputLiveService = containerProvider.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            PauseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Pause(),
                canExecuteMethod: () => outputLiveService.IsPlaying);
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand LowerCommand { get; }

        public bool OthersMuted
        {
            get { return othersMuted; }
            set { SetProperty(ref othersMuted, value); }
        }

        public DelegateCommand PauseCommand { get; }

        public DelegateCommand RaiseCommand { get; }

        #endregion Public Properties

        #region Private Methods

        private void OnOutputStarted(IOutputService outputService)
        {
            if (outputService == outputLiveService)
            {
                UpdateCommands();
            }
        }

        private void OnOutputStopped(IOutputService outputService)
        {
            if (outputService == outputLiveService)
            {
                UpdateCommands();
            }
        }

        private void UpdateCommands()
        {
            PauseCommand.RaiseCanExecuteChanged();
        }

        #endregion Private Methods
    }
}