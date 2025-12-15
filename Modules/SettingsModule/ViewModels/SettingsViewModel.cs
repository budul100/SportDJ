using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.SettingsModule.ViewModels
{
    public class SettingsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IDialogService dialogService;
        private readonly IEventAggregator eventAggregator;
        private readonly FilePickerFileType[] fileTypeFilter = [new(Constants.SettingsExtension)];
        private readonly IOutputService outputService;
        private readonly Output outputSettings;
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private Device device;
        private bool devicesNotAvailable;
        private bool isUpdate;
        private IStorageFolder startLocation;

        #endregion Private Fields

        #region Public Constructors

        public SettingsViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IDialogService dialogService, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
            : base(regionManager)
        {
            this.eventAggregator = eventAggregator;
            this.dialogService = dialogService;
            this.settingsService = settingsService;
            this.sessionService = sessionService;

            SettingsLoadCommand = new DelegateCommand(
                async () => await LoadSettingsAsync());

            SettingsSaveCommand = new DelegateCommand(
                async () => await SaveSettingsAsync());

            UpdateDevicesCommand = new DelegateCommand(
                executeMethod: UpdateDevices);

            outputService = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            outputSettings = sessionService?.Contents?.Outputs?
                .SingleOrDefault(o => o.Type == OutputType.Live);

            eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
                action: SetStartLocation,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => DevicesNotAvailable = true,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => o == outputService);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => DevicesNotAvailable = false,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => o == outputService);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateDevices(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => o == outputService);

            eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: InitializeOptions,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
                action: UpdateOptions,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

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

        public ObservableCollection<Device> Devices { get; private set; }

        public bool DevicesNotAvailable
        {
            get { return devicesNotAvailable; }
            set { SetProperty(ref devicesNotAvailable, value); }
        }

        public bool IsActive => Options != default
            && sessionService.Contents.SettingsVisible;

        public Options Options => settingsService?.Contents?.Options;

        public DelegateCommand SettingsLoadCommand { get; }

        public DelegateCommand SettingsSaveCommand { get; }

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

        private void InitializeOptions()
        {
            RaisePropertyChanged(nameof(Options));

            RaisePropertyChanged(nameof(Volume));
            RaisePropertyChanged(nameof(VolumePercentageExternals));
            RaisePropertyChanged(nameof(VolumePercentageBackground));

            RaisePropertyChanged(nameof(IsActive));
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

        private void SetStartLocation()
        {
            Task.Run(async () => startLocation = await dialogService.GetFolderAsync(sessionService?.Contents?.SettingsPath));
        }

        private void UpdateDevices()
        {
            if (!isUpdate)
            {
                // TODO Show settings if not opened

                isUpdate = true;

                outputService.Update();

                Devices = new ObservableCollection<Device>(outputService.Devices
                    .OrderByDescending(d => d.Type == DeviceType.Asio)
                    .ThenBy(d => d.Name));
                RaisePropertyChanged(nameof(Devices));

                Device = Devices
                    .SingleOrDefault(d => d.Name == outputService.Device?.Name);

                isUpdate = false;
            }

            DevicesNotAvailable = false;
        }

        private void UpdateOptions()
        {
            settingsService.Save();

            eventAggregator.GetEvent<SettingsUpdatedEvent<Set>>().Publish();

            RaisePropertyChanged(nameof(IsActive));
        }

        #endregion Private Methods
    }
}