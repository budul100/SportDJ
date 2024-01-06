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
        private readonly IVolumeService volumeService;

        private bool othersMuted;
        private bool volumeMonitoringActive;

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

            volumeService = containerProvider?.Resolve<IVolumeService>(
                name: nameof(OutputType.Live));

            PauseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Pause(),
                canExecuteMethod: () => IsActive);

            VolumeLowerCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Lower(),
                canExecuteMethod: () => IsActive);

            VolumeRaiseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService.Raise(),
                canExecuteMethod: () => IsActive);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive => outputLiveService.IsPlaying || VolumeMonitoringActive;

        public bool OthersMuted
        {
            get { return othersMuted; }
            set { SetProperty(ref othersMuted, value); }
        }

        public DelegateCommand PauseCommand { get; }

        public DelegateCommand VolumeLowerCommand { get; }

        public bool VolumeMonitoringActive
        {
            get { return volumeService.IsActive; }
            set
            {
                if (value)
                {
                    volumeService.Activate();
                }
                else
                {
                    volumeService.Deactivate();
                }

                SetProperty(ref volumeMonitoringActive, value);

                UpdateCommands();
            }
        }

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
            RaisePropertyChanged(nameof(IsActive));

            PauseCommand.RaiseCanExecuteChanged();
            VolumeLowerCommand.RaiseCanExecuteChanged();
            VolumeRaiseCommand.RaiseCanExecuteChanged();
        }

        #endregion Private Methods
    }
}