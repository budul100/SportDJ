using System;
using System.Collections.Generic;
using System.Linq;
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
using SportDJ.Commons.Events.Signal;
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

        private readonly IMixerService mixerService;
        private readonly IRegionNavigationService navigationService;
        private readonly IOutputService outputLiveService;
        private readonly IEnumerable<IMonitorService> monitorServices;

        private bool isListening;
        private bool isLoweredRequested;
        private bool isVolumePressed;
        private Options options;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IRegionNavigationService navigationService,
            IStorageService<Set> settingsService, IEnumerable<IMonitorService> monitoringServices)
            : base(regionManager)
        {
            this.navigationService = navigationService;
            this.monitorServices = monitoringServices;

            mixerService = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));

            outputLiveService = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            LoopCommand = new DelegateCommand(
                executeMethod: SwitchLoop);

            MonitoringCommand = new DelegateCommand(
                executeMethod: SwitchMonitoring);

            VolumeLowerCommand = new DelegateCommand(
                executeMethod: LowerOutput,
                canExecuteMethod: () => IsActive);

            VolumeRaiseCommand = new DelegateCommand(
                executeMethod: RaiseOutput,
                canExecuteMethod: () => IsActive);

            PauseCommand = new DelegateCommand(
                executeMethod: () => outputLiveService?.Pause(),
                canExecuteMethod: () => IsActive);

            eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: () => options = settingsService?.Contents?.Options,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
                action: UpdateOptions,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<MonitorUpdatedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: v => monitoringServices?.Contains(v) == true);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<OutputFadedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<LoopReadyEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<LoopActivatedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<LoopDeactivatedEvent>().Subscribe(
                action: _ => UpdateControls(),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: m => mixerService == m);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: OnKeyPressed,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<KeyReleasedEvent>().Subscribe(
                action: OnKeyReleased,
                keepSubscriberReferenceAlive: true);

            InitializeMenus();
            UpdateControls();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler ListenerCheckEvent;

        #endregion Public Events

        #region Public Properties

        public bool HasKeyPauseOutput => !string.IsNullOrWhiteSpace(options?.KeyCodeOutputPause);

        public bool HasKeyTalkVolume => !string.IsNullOrWhiteSpace(options?.KeyCodeVolumeTalk);

        public bool IsActive => (outputLiveService?.IsPlaying == true) || IsMonitoring;

        public bool IsLoop => mixerService?.IsLoop == true;

        public bool IsLoopEnabled => (outputLiveService?.IsPlaying == true)
            && (mixerService?.IsLoopReady == true);

        public bool IsMonitoring => monitorServices?.Any(s => s.IsActive) == true;

        public bool IsMonitoringEnabled => monitorServices?.Any(s => s.IsReady) == true;

        public bool IsVolumeLowered => isLoweredRequested
            && outputLiveService?.IsLowered == true;

        public bool IsVolumePressed
        {
            get => isVolumePressed;
            set => SetProperty(ref isVolumePressed, value);
        }

        public string KeyPauseOutput => options?.KeyCodeOutputPause.GetKey();

        public string KeyTalkVolume => options?.KeyCodeVolumeTalk.GetKey();

        public DelegateCommand LoopCommand { get; }

        public DelegateCommand MonitoringCommand { get; }

        public DelegateCommand PauseCommand { get; }

        public DelegateCommand VolumeLowerCommand { get; }

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
            }
        }

        private void LowerOutput()
        {
            isLoweredRequested = true;

            outputLiveService.Lower();
        }

        private void OnKeyPressed(string keyCode)
        {
            if (keyCode == options?.KeyCodeOutputPause)
            {
                outputLiveService.Pause();
            }
            else if (keyCode == options?.KeyCodeVolumeTalk)
            {
                LowerOutput();
            }
        }

        private void OnKeyReleased(string keyCode)
        {
            if (keyCode == options?.KeyCodeVolumeTalk)
            {
                RaiseOutput();
            }
        }

        private void RaiseOutput()
        {
            isLoweredRequested = false;

            outputLiveService.Raise();
        }

        private void SwitchLoop()
        {
            if (IsLoop)
            {
                mixerService?.DeactivateLoop();
            }
            else
            {
                mixerService?.ActivateLoop();
            }

            UpdateControls();
        }

        private void SwitchMonitoring()
        {
            if (monitorServices?.Any() == true)
            {
                foreach (var volumeService in monitorServices)
                {
                    if (IsMonitoring)
                    {
                        volumeService.Deactivate();
                    }
                    else
                    {
                        volumeService.Activate();
                    }
                }
            }

            UpdateControls();
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
            RaisePropertyChanged(nameof(IsActive));

            RaisePropertyChanged(nameof(IsLoop));
            RaisePropertyChanged(nameof(IsLoopEnabled));

            RaisePropertyChanged(nameof(IsMonitoring));
            RaisePropertyChanged(nameof(IsMonitoringEnabled));

            RaisePropertyChanged(nameof(IsVolumeLowered));

            VolumeLowerCommand.RaiseCanExecuteChanged();
            VolumeRaiseCommand.RaiseCanExecuteChanged();

            PauseCommand.RaiseCanExecuteChanged();
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