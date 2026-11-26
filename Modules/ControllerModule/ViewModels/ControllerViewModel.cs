using System;
using System.Collections.Generic;
using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.ControllerModule.ViewModels;

public class ControllerViewModel
    : BindableBase
{
    #region Private Fields

    private readonly IEventAggregator eventAggregator;
    private readonly IKeyboardService keyboardService;
    private readonly IMixerService mixerService;
    private readonly IEnumerable<IMonitorService> monitorServices;
    private readonly IOutputService outputLiveService;
    private readonly IPanelService panelService;

    private bool isLoweredRequested;
    private bool isSettingsVisible;
    private bool isVolumePressed;
    private Options options;

    #endregion Private Fields

    #region Public Constructors

    public ControllerViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        IStorageService<Set> settingsService, IEnumerable<IMonitorService> monitoringServices,
        IPanelService panelService, IKeyboardService keyboardService)
    {
        this.eventAggregator = eventAggregator;
        this.keyboardService = keyboardService;
        this.monitorServices = monitoringServices;
        this.panelService = panelService;

        mixerService = containerProvider?.Resolve<IMixerService>(
            name: nameof(MixerType.Base));

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

        ShowSettingsCommand = new DelegateCommand(
            executeMethod: ShowSettings);

        // One pane for all settings: the button is only checked while the pane shows the settings of the control keys
        eventAggregator?.GetEvent<ControllerVisibleEvent>().Subscribe(
            action: () => IsSettingsVisible = true,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<PlayerVisibleEvent>().Subscribe(
            action: _ => IsSettingsVisible = false,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<TabVisibleEvent>().Subscribe(
            action: _ => IsSettingsVisible = false,
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: _ => UpdatePanelButtons(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
            action: () =>
            {
                options = settingsService?.Contents?.Options;

                // Without this the key badges stay empty after loading
                UpdateOptions();
            },
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
            action: UpdateOptions,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<MonitorUpdatedEvent>().Subscribe(
            action: _ => UpdateControls(),
            threadOption: ThreadOption.UIThread,
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

        UpdateControls();
    }

    #endregion Public Constructors

    #region Public Properties

    public bool HasKeyPauseOutput => !string.IsNullOrWhiteSpace(options?.KeyCodeOutputPause);

    public bool HasKeyTalkVolume => !string.IsNullOrWhiteSpace(options?.KeyCodeVolumeTalk);

    public bool IsActive => (outputLiveService?.IsPlaying == true) || IsMonitoring;

    public bool IsLoop => mixerService?.IsLoop == true;

    public bool IsLoopEnabled => (outputLiveService?.IsPlaying == true)
        && (mixerService?.IsLoopReady == true);

    public bool IsMonitoring => monitorServices?.Any(s => s.IsActive) == true;

    public bool IsMonitoringEnabled => monitorServices?.Any(s => s.IsReady) == true;

    public bool IsSettingsVisible
    {
        get => isSettingsVisible;
        set => SetProperty(ref isSettingsVisible, value);
    }

    public bool IsVolumeLowered => isLoweredRequested
        && outputLiveService?.IsLowered == true;

    public bool IsVolumePressed
    {
        get => isVolumePressed;
        set => SetProperty(ref isVolumePressed, value);
    }

    public string KeyCodePauseOutput => options?.KeyCodeOutputPause;

    public string KeyCodeTalkVolume => options?.KeyCodeVolumeTalk;

    public DelegateCommand LoopCommand { get; }

    public DelegateCommand MonitoringCommand { get; }

    public DelegateCommand PauseCommand { get; }

    public DelegateCommand ShowSettingsCommand { get; }

    public DelegateCommand VolumeLowerCommand { get; }

    public DelegateCommand VolumeRaiseCommand { get; }

    #endregion Public Properties

    #region Private Methods

    private void LowerOutput()
    {
        isLoweredRequested = true;

        outputLiveService?.Lower();
    }

    private void OnKeyPressed(string keyCode)
    {
        // While anything waits for a key to be assigned, the key must not trigger a control
        if (keyboardService?.IsAny == true)
        {
            return;
        }

        if (keyCode == options?.KeyCodeOutputPause)
        {
            outputLiveService?.Pause();
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

        outputLiveService?.Raise();
    }

    // One pane for all settings, the same button opens and closes it like at a player
    private void ShowSettings()
    {
        if (IsSettingsVisible)
        {
            panelService?.Deactivate<IPlayerService>();
        }
        else
        {
            eventAggregator?.GetEvent<ControllerVisibleEvent>().Publish();

            panelService?.Activate<IControllerService>();
        }
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
        RaisePropertyChanged(nameof(KeyCodePauseOutput));
        RaisePropertyChanged(nameof(HasKeyPauseOutput));

        RaisePropertyChanged(nameof(KeyCodeTalkVolume));
        RaisePropertyChanged(nameof(HasKeyTalkVolume));
    }

    private void UpdatePanelButtons()
    {
        if (panelService?.IsVisible<IPlayerService>() != true)
        {
            IsSettingsVisible = false;
        }
    }

    #endregion Private Methods
}
