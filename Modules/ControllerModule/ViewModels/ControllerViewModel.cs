using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.ControllerModule.ViewModels
{
    public class ControllerViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IOutputService outputLiveService;
        private readonly IVolumeService volumeService;

        private Options options;
        private bool othersMuted;
        private bool volumeMonitoringActive;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService)
            : base(regionManager)
        {
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

            eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: () => options = settingsService?.Contents?.Options,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
                action: UpdateOptions,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<VolumeUpdatedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: v => volumeService == v);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: k => OnKeyPressed(k),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<KeyReleasedEvent>().Subscribe(
                action: k => OnKeyReleased(k),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool HasKeyPauseOutput => !string.IsNullOrWhiteSpace(options?.KeyCodeOutputPause);

        public bool HasKeyTalkVolume => !string.IsNullOrWhiteSpace(options?.KeyCodeVolumeTalk);

        public bool IsActive => outputLiveService.IsPlaying || VolumeMonitoringActive;

        public string KeyPauseOutput => options?.KeyCodeOutputPause.GetKey();

        public string KeyTalkVolume => options?.KeyCodeVolumeTalk.GetKey();

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

                UpdateContent();
            }
        }

        public DelegateCommand VolumeRaiseCommand { get; }

        #endregion Public Properties

        #region Private Methods

        private void OnKeyPressed(string keyCode)
        {
            if (keyCode == options?.KeyCodeOutputPause)
            {
                outputLiveService.Pause();
            }
            else if (keyCode == options?.KeyCodeVolumeTalk)
            {
                outputLiveService.Lower();
            }
        }

        private void OnKeyReleased(string keyCode)
        {
            if (keyCode == options?.KeyCodeVolumeTalk)
            {
                outputLiveService.Raise();
            }
        }

        private void UpdateContent()
        {
            RaisePropertyChanged(nameof(VolumeMonitoringActive));
            RaisePropertyChanged(nameof(IsActive));

            PauseCommand.RaiseCanExecuteChanged();
            VolumeLowerCommand.RaiseCanExecuteChanged();
            VolumeRaiseCommand.RaiseCanExecuteChanged();
        }

        private void UpdateOptions()
        {
            RaisePropertyChanged(nameof(KeyPauseOutput));
            RaisePropertyChanged(nameof(HasKeyPauseOutput));

            RaisePropertyChanged(nameof(KeyTalkVolume));
            RaisePropertyChanged(nameof(HasKeyTalkVolume));
        }

        #endregion Private Methods
    }
}