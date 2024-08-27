using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
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
        private readonly FilePickerFileType[] fileTypeFilter = new FilePickerFileType[] { new(Constants.SettingsExtension) };
        private readonly IOutputService outputService;
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private string device;
        private bool devicesNotAvailable;
        private bool isUpdate;
        private Output outputSettings;
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
                executeMethod: () => UpdateDevices());

            outputService = containerProvider.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            eventAggregator?.GetEvent<AppStartedEvent>().Subscribe(
                action: () => SetStartLocation(),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateDevices(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => o == outputService);

            eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: InitializeOptions,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsUpdatedEvent<Session>>().Subscribe(
                action: () => UpdateOptions(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public string Device
        {
            get { return device; }
            set
            {
                if (value != device)
                {
                    SetProperty(ref device, value);

                    if (!isUpdate)
                    {
                        DevicesNotAvailable = true;

                        outputService.Set(device);

                        DevicesNotAvailable = false;
                    }
                }
            }
        }

        public ObservableCollection<string> Devices { get; private set; }

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
            get { return outputSettings?.Volume ?? 0; }
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

        public int VolumePercentageApps
        {
            get { return outputSettings?.VolumePercentageApps ?? 0; }
            set
            {
                if (outputSettings != default
                    && value != outputSettings.VolumePercentageApps)
                {
                    outputSettings.VolumePercentageApps = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(VolumePercentageApps));
                }
            }
        }

        public int VolumePercentageBackground
        {
            get { return outputSettings?.VolumePercentageBackground ?? 0; }
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

        #endregion Public Properties

        #region Private Methods

        private void InitializeOptions()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                settingsService.Contents.Options.DisableKeyHook = true;
            }

            outputSettings = settingsService.Contents.Outputs?
                .SingleOrDefault(o => o.Type == OutputType.Live);

            RaisePropertyChanged(nameof(Options));

            RaisePropertyChanged(nameof(Volume));
            RaisePropertyChanged(nameof(VolumePercentageApps));
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
                    .FirstOrDefault(p => File.Exists(p));

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
            isUpdate = true;

            Devices = new ObservableCollection<string>(outputService.GetDevices()
                .Select(d => d.Value));

            RaisePropertyChanged(nameof(Devices));

            Device = outputService.DeviceName;

            isUpdate = false;
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