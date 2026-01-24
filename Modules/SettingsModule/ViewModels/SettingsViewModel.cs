using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.Modules.SettingsModule.ViewModels
{
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

            SettingsSaveCommand = new DelegateCommand(
                async () => await SaveSettingsAsync());

            UpdateDevicesCommand = new DelegateCommand(
                executeMethod: UpdateDevices);

            UpdateControllersCommand = new DelegateCommand(
                executeMethod: UpdateControllers);

            outputService = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            outputSettings = sessionService?.Contents?.Outputs?
                .SingleOrDefault(o => o.Type == OutputType.Live);

            eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
                action: InitializeSettings,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<PanelUpdatedEvent>().Subscribe(
                action: _ => RefreshSettings(),
                keepSubscriberReferenceAlive: true,
                threadOption: ThreadOption.PublisherThread,
                filter: p => p.Type == typeof(IStorageService<Session>));

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
                action: SaveSettings,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => DevicesNotAvailable = true,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => DevicesNotAvailable = false,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<ControllerUpdatedEvent>().Subscribe(
                action: SetController,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<ControllersUpdatedEvent>().Subscribe(
                action: SetControllers,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<DevicesUpdatedEvent>().Subscribe(
                action: SetDevices,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => SetDevice(),
                threadOption: ThreadOption.PublisherThread,
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

        public Options Options => settingsService?.Contents?.Options;

        public DelegateCommand SettingsLoadCommand { get; }

        public DelegateCommand SettingsSaveCommand { get; }

        public DelegateCommand UpdateControllersCommand { get; }

        public DelegateCommand UpdateDevicesCommand { get; }

        public float Volume
        {
            get { return outputSettings?.Volume ?? Constants.VolumeMax; }
            set
            {
                if (outputSettings != default
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

        private void InitializeSettings()
        {
            UpdateDevices();
            UpdateControllers();

            SetStartLocation();
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

                    SetStartLocation();
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
                title: Texts.SettingsLoadHeader,
                defaultExtension: Constants.SettingsExtension,
                fileTypeChoices: fileTypeFilter,
                showOverwritePrompt: true,
                suggestedFileName: fileName,
                suggestedStartLocation: startLocation);

            if (path != default)
            {
                sessionService.Contents.SettingsPath = path.Path.LocalPath;

                sessionService.Save();

                settingsService.Save(path.Path.LocalPath);

                SetStartLocation();
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

            if (Device == default)
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
            }
        }

        private void SetStartLocation()
        {
            Task.Run(async () => startLocation = await dialogService
                .GetFolderAsync(sessionService?.Contents?.SettingsPath));
        }

        private void UpdateControllers()
        {
            if (!isUpdateControllers)
            {
                isUpdateControllers = true;

                controllerService?.Update();

                isUpdateControllers = false;
            }
        }

        private void UpdateDevices()
        {
            if (!isUpdateDevices)
            {
                isUpdateDevices = true;

                outputService?.Update();

                isUpdateDevices = false;
            }
        }

        #endregion Private Methods
    }
}