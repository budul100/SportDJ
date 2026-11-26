using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.SettingsModule.ViewModels;

public class SettingsViewModel
    : BindableBase
{
    #region Private Fields

    private readonly IControllerService controllerService;
    private readonly IDialogService dialogService;
    private readonly IEventAggregator eventAggregator;
    private readonly FilePickerFileType[] fileTypeFilter = [new(Constants.SettingsExtension)];
    private readonly IOutputService outputService;
    private readonly Output outputSettings;
    private readonly IPanelService panelService;
    private readonly IStorageService<Session> sessionService;
    private readonly IStorageService<Set> settingsService;

    private Controller controller;
    private bool controllersNotAvailable;
    private Device device;
    private bool devicesNotAvailable;
    private bool isControllersLoading;
    private bool isDevicesLoading;
    private bool isUpdateControllers;
    private bool isUpdateDevices;
    private IStorageFolder startLocation;

    #endregion Private Fields

    #region Public Constructors

    public SettingsViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IDialogService dialogService, IPanelService panelService, IStorageService<Set> settingsService,
        IStorageService<Session> sessionService, IControllerService controllerService)
    {
        this.eventAggregator = eventAggregator;
        this.dialogService = dialogService;
        this.panelService = panelService;
        this.settingsService = settingsService;
        this.sessionService = sessionService;
        this.controllerService = controllerService;

        SettingsLoadCommand = new DelegateCommand(
            async () => await LoadSettingsAsync());

        SettingsNewCommand = new DelegateCommand(
            async () => await NewSessionAsync());

        SettingsSaveCommand = new DelegateCommand(
            async () => await SaveSettingsAsync());

        UpdateDevicesCommand = new DelegateCommand(
            async () => await UpdateDevicesAsync());

        UpdateControllersCommand = new DelegateCommand(
            async () => await UpdateControllersAsync());

        outputService = containerProvider?.Resolve<IOutputService>(
            name: nameof(OutputType.Live));

        outputSettings = sessionService?.Contents?.Outputs?
            .SingleOrDefault(o => o.Type == OutputType.Live);

        eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
            action: () => _ = InitializeSettingsAsync(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
            action: _ => RefreshSettings(),
            keepSubscriberReferenceAlive: true,
            threadOption: ThreadOption.UIThread,
            filter: p => p.Type == typeof(IStorageService<Session>));

        eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
            action: SaveSettings,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
            action: _ => DevicesNotAvailable = true,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
            action: _ => DevicesNotAvailable = false,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<ControllerUpdatedEvent>().Subscribe(
            action: SetController,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<ControllersUpdatedEvent>().Subscribe(
            action: SetControllers,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<DevicesUpdatedEvent>().Subscribe(
            action: SetDevices,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
            action: _ => SetDevice(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);
    }

    #endregion Public Constructors

    #region Public Properties

    public Controller Controller
    {
        get { return controller; }
        set
        {
            if (value != controller)
            {
                SetProperty(ref controller, value);

                if (controller != default)
                {
                    ControllersNotAvailable = true;

                    controllerService.Set(controller.Name);
                }
            }
        }
    }

    public ObservableCollection<Controller> Controllers { get; private set; } = [];

    public bool ControllersNotAvailable
    {
        get { return controllersNotAvailable; }
        set { SetProperty(ref controllersNotAvailable, value); }
    }

    public Device Device
    {
        get { return device; }
        set
        {
            if (value != device)
            {
                SetProperty(ref device, value);

                if (device != default)
                {
                    DevicesNotAvailable = true;

                    outputService.Set(device.Name);
                }
            }
        }
    }

    public ObservableCollection<Device> Devices { get; private set; } = [];

    public bool DevicesNotAvailable
    {
        get { return devicesNotAvailable; }
        set { SetProperty(ref devicesNotAvailable, value); }
    }

    public bool IsActive => Options != default
        && panelService.IsVisible<IStorageService<Session>>();

    public bool IsControllersLoading
    {
        get { return isControllersLoading; }
        private set { SetProperty(ref isControllersLoading, value); }
    }

    public bool IsDevicesLoading
    {
        get { return isDevicesLoading; }
        private set { SetProperty(ref isDevicesLoading, value); }
    }

    public Options Options => settingsService?.Contents?.Options;

    public DelegateCommand SettingsLoadCommand { get; }

    public DelegateCommand SettingsNewCommand { get; }

    public DelegateCommand SettingsSaveCommand { get; }

    public DelegateCommand UpdateControllersCommand { get; }

    public DelegateCommand UpdateDevicesCommand { get; }

    public float Volume
    {
        get { return outputSettings?.Volume ?? Constants.VolumeMax; }
        set
        {
            if (outputSettings != default
                && outputService != default
                && value != outputService.Volume)
            {
                outputService.Volume = value;

                settingsService.Save();

                RaisePropertyChanged(nameof(Volume));
            }
        }
    }

    public int VolumePercentageBackground
    {
        get { return outputSettings?.VolumePercentageBackground ?? Defaults.VolumePercentageBackground; }
        set
        {
            if (outputSettings != default
                && value != outputSettings.VolumePercentageBackground)
            {
                outputSettings.VolumePercentageBackground = value;

                settingsService.Save();

                RaisePropertyChanged(nameof(VolumePercentageBackground));
            }
        }
    }

    public int VolumePercentageExternals
    {
        get { return outputSettings?.VolumePercentageExternals ?? Defaults.VolumePercentageExternals; }
        set
        {
            if (outputSettings != default
                && value != outputSettings.VolumePercentageExternals)
            {
                outputSettings.VolumePercentageExternals = value;

                settingsService.Save();

                RaisePropertyChanged(nameof(VolumePercentageExternals));
            }
        }
    }

    #endregion Public Properties

    #region Private Methods

    private async Task<bool> ConfirmOverwriteAsync(string path, bool isCurrentFileExcepted)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        var fullPath = Path.GetFullPath(path);

        if (isCurrentFileExcepted
            && string.Equals(
                a: fullPath,
                b: settingsService.Path,
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var result = await dialogService.GetMessageBoxResultAsync(
            contentMessage: string.Format(Texts.SettingsOverwriteMessage, fullPath),
            contentTitle: Texts.SettingsOverwriteHeader,
            buttonDefinitions: ButtonEnum.YesNo,
            enterDefaultButton: ClickEnum.No);

        return result == ButtonResult.Yes;
    }

    // The output service has already enumerated the devices while it was created, so they are only taken over here.
    // The controllers are enumerated in the background, the UI stays usable meanwhile.
    private async Task InitializeSettingsAsync()
    {
        SetDevices();

        await UpdateControllersAsync();

        await SetStartLocationAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var paths = await dialogService.OpenFilePickerAsync(
            title: Texts.SettingsLoadHeader,
            fileTypeFilter: fileTypeFilter,
            allowMultiple: false,
            startLocation: startLocation);

        if (paths?.Any() == true)
        {
            var path = paths
                .Select(p => p.Path.LocalPath)
                .FirstOrDefault(File.Exists);

            if (path != default)
            {
                sessionService.Contents.SettingsPath = path;

                sessionService.Save();

                settingsService.Load(path);

                await SetStartLocationAsync();
            }
        }
    }

    private async Task NewSessionAsync()
    {
        var result = await dialogService.GetMessageBoxResultAsync(
            contentMessage: Texts.SettingsNewMessage,
            contentTitle: Texts.SettingsNewHeader,
            buttonDefinitions: ButtonEnum.YesNo,
            enterDefaultButton: ClickEnum.No);

        if (result == ButtonResult.Yes)
        {
            // The audio paths of a session are stored relative to its settings file, so the user decides where it lives
            var file = await dialogService.SaveFilePickerAsync(
                title: Texts.SettingsNewHeader,
                defaultExtension: Constants.SettingsExtension,
                fileTypeChoices: fileTypeFilter,
                showOverwritePrompt: false,
                suggestedFileName: Defaults.FileNameSettings,
                suggestedStartLocation: startLocation);

            // Here even the current file is asked for, because it would be emptied
            if (file != default
                && await ConfirmOverwriteAsync(file.Path.LocalPath, isCurrentFileExcepted: false))
            {
                var path = file.Path.LocalPath;

                sessionService.Contents.SettingsPath = path;

                sessionService.Save();

                settingsService.New(path);

                settingsService.Save();

                await SetStartLocationAsync();
            }
        }
    }

    private void RefreshSettings()
    {
        if (panelService.IsVisible<IStorageService<Session>>())
        {
            RaisePropertyChanged(nameof(Options));

            RaisePropertyChanged(nameof(Volume));
            RaisePropertyChanged(nameof(VolumePercentageExternals));
            RaisePropertyChanged(nameof(VolumePercentageBackground));

            RaisePropertyChanged(nameof(IsActive));
        }
    }

    private void SaveSettings()
    {
        settingsService.Save();

        eventAggregator.GetEvent<SettingsUpdatedEvent<Set>>().Publish();

        RaisePropertyChanged(nameof(IsActive));
    }

    private async Task SaveSettingsAsync()
    {
        var fileName = Path.GetFileNameWithoutExtension(sessionService?.Contents?.SettingsPath);

        var path = await dialogService.SaveFilePickerAsync(
            title: Texts.SettingsSaveHeader,
            defaultExtension: Constants.SettingsExtension,
            fileTypeChoices: fileTypeFilter,
            showOverwritePrompt: false,
            suggestedFileName: fileName,
            suggestedStartLocation: startLocation);

        // The file in use is overwritten without asking, every other existing file only after a confirmation
        if (path != default
            && await ConfirmOverwriteAsync(path.Path.LocalPath, isCurrentFileExcepted: true))
        {
            sessionService.Contents.SettingsPath = path.Path.LocalPath;

            sessionService.Save();

            settingsService.Save(path.Path.LocalPath);

            await SetStartLocationAsync();
        }
    }

    private void SetController()
    {
        Controller = Controllers?
            .SingleOrDefault(d => d.Name == controllerService?.Controller?.Name);

        ControllersNotAvailable = false;
    }

    private void SetControllers()
    {
        var givenSet = Controllers.ToHashSet();
        var updateSet = controllerService?.Controllers?.ToHashSet();

        if (updateSet?.SetEquals(givenSet) != true)
        {
            Controllers.Clear();

            if (updateSet?.Count > 0)
            {
                var ordereds = updateSet
                    .OrderBy(c => c.Name).ToArray();

                foreach (var ordered in ordereds)
                {
                    Controllers.Add(ordered);
                }
            }

            RaisePropertyChanged(nameof(Controllers));
        }
    }

    private void SetDevice()
    {
        Device = Devices?
            .SingleOrDefault(d => d.Name == outputService?.Device?.Name);

        if (Device == default
            && Devices?.Any() == true)
        {
            panelService.Activate<IStorageService<Session>>();
        }

        DevicesNotAvailable = false;
    }

    private void SetDevices()
    {
        var givenSet = Devices.ToHashSet();
        var updateSet = outputService?.Devices?.ToHashSet();

        if (updateSet?.SetEquals(givenSet) != true)
        {
            Devices.Clear();

            if (updateSet?.Count > 0)
            {
                var ordereds = updateSet
                    .OrderByDescending(d => d.Type == DeviceType.Asio)
                    .ThenBy(d => d.Name).ToArray();

                foreach (var ordered in ordereds)
                {
                    Devices.Add(ordered);
                }
            }

            RaisePropertyChanged(nameof(Devices));

            SetDevice();
        }
    }

    private async Task SetStartLocationAsync()
    {
        // The storage provider belongs to the UI thread, so no Task.Run here
        try
        {
            startLocation = await dialogService
                .GetFolderAsync(sessionService?.Contents?.SettingsPath);
        }
        catch (Exception)
        {
            startLocation = default;
        }
    }

    private async Task UpdateControllersAsync()
    {
        if (!isUpdateControllers)
        {
            isUpdateControllers = true;
            IsControllersLoading = true;

            try
            {
                await Task.Run(() => controllerService?.Update());
            }
            catch (Exception)
            {
                // TODO: Log exception, the list keeps its previous content
            }
            finally
            {
                isUpdateControllers = false;
                IsControllersLoading = false;
            }
        }
    }

    private async Task UpdateDevicesAsync()
    {
        if (!isUpdateDevices)
        {
            isUpdateDevices = true;
            IsDevicesLoading = true;

            try
            {
                await Task.Run(() => outputService?.Update());
            }
            catch (Exception)
            {
                // TODO: Log exception, the list keeps its previous content
            }
            finally
            {
                isUpdateDevices = false;
                IsDevicesLoading = false;
            }
        }
    }

    #endregion Private Methods
}