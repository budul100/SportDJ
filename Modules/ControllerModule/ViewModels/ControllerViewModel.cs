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

            outputLiveService = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            PauseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Pause(),
                canExecuteMethod: () => outputLiveService.IsPlaying);

            VolumeLowerCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Lower(),
                canExecuteMethod: () => outputLiveService.IsPlaying);

            VolumeRaiseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Raise(),
                canExecuteMethod: () => outputLiveService.IsPlaying);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsPlaying => outputLiveService.IsPlaying;

        public bool OthersMuted
        {
            get { return othersMuted; }
            set { SetProperty(ref othersMuted, value); }
        }

        public DelegateCommand PauseCommand { get; }

        public DelegateCommand VolumeLowerCommand { get; }

        public DelegateCommand VolumeRaiseCommand { get; }

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
            RaisePropertyChanged(nameof(IsPlaying));

            PauseCommand.RaiseCanExecuteChanged();
            VolumeLowerCommand.RaiseCanExecuteChanged();
            VolumeRaiseCommand.RaiseCanExecuteChanged();
        }

        #endregion Private Methods
    }
}