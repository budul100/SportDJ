using Material.Icons;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;
using System;

namespace SportDJ.Modules.ControllerModule.ViewModels
{
    public class ControllerViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IHookService hookService;
        private readonly IRegionNavigationService navigationService;
        private readonly IOutputService outputLiveService;
        private readonly IVolumeService volumeService;

        private bool isListening;
        private Options options;
        private bool othersMuted;
        private bool volumeMonitoringActive;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IRegionNavigationService navigationService, IHookService hookService,
            IStorageService<Set> settingsService)
            : base(regionManager)
        {
            this.navigationService = navigationService;
            this.hookService = hookService;

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
                action: _ => UpdateControls(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: v => volumeService == v);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: OnKeyPressed,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<KeyReleasedEvent>().Subscribe(
                action: OnKeyReleased,
                keepSubscriberReferenceAlive: true);

            InitializeMenus();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler ListenerCheckEvent;

        #endregion Public Events

        #region Public Properties

        public bool HasKeyPauseOutput => !string.IsNullOrWhiteSpace(options?.KeyCodeOutputPause);

        public bool HasKeyTalkVolume => !string.IsNullOrWhiteSpace(options?.KeyCodeVolumeTalk);

        public bool IsActive => (outputLiveService?.IsPlaying == true) || VolumeMonitoringActive;

        public bool IsVolumeMonitoringAvailable => volumeService?.IsInitialized == true;

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
            get { return volumeService?.IsActive == true; }
            set
            {
                if (volumeService != default)
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
                }

                UpdateControls();
            }
        }

        public DelegateCommand VolumeRaiseCommand { get; }

        #endregion Public Properties

        #region Private Methods

        private void InitializeMenus()
        {
            var navigationParameters = new NavigationParameters
            {
                { "header", Texts.KeyListenHeader },
                { "executeCallback", () => ListenHook() },
                { "isCheckedGetter", isListening },
                { "isCheckedEvent", ListenerCheckEvent },
                { "icon", MaterialIconKind.KeyboardOutline }
            };

            navigationService?.RequestNavigate(
                target: nameof(RegionType.MenuRegion),
                navigationParameters: navigationParameters);
        }

        private void ListenHook()
        {
            if (!isListening)
            {
                isListening = true;

                //hookService.Listen(k => ReceiveHook(k));
            }
        }

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

        //public bool HasKey => !string.IsNullOrWhiteSpace(Key);

        //public string Key => KeyCode.GetKey();

        //public string KeyCode
        //{
        //    get { return keyable?.KeyCode; }

        //    set
        //    {
        //        if (keyable != default
        //            && value != keyable.KeyCode)
        //        {
        //            keyable.KeyCode = value;

        //            settingsService.Save();

        //            RaisePropertyChanged(nameof(KeyCode));
        //            RaisePropertyChanged(nameof(HasKey));
        //            RaisePropertyChanged(nameof(Key));
        //        }
        //    }
        //}

        //private void ReceiveHook(string keyCode)
        //{
        //    if (isListening)
        //    {
        //        KeyCode = keyCode;
        //        isListening = false;

        //        listenChecker.Invoke();

        //        CloseMenu();
        //    }
        //}

        private void UpdateControls()
        {
            RaisePropertyChanged(nameof(IsVolumeMonitoringAvailable));
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