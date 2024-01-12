using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Enums;
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

        private readonly IOutputService outputService;
        private readonly Output outputSettings;
        private readonly IStorageService<Session> sessionService;
        private readonly IStorageService<Set> settingsService;

        private string device;
        private bool devicesNotAvailable;
        private bool isUpdate;

        #endregion Private Fields

        #region Public Constructors

        public SettingsViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
            : base(regionManager)
        {
            this.settingsService = settingsService;
            this.sessionService = sessionService;

            UpdateDevicesCommand = new DelegateCommand(
                executeMethod: () => UpdateDevices());

            outputService = containerProvider.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            outputSettings = settingsService.Contents.Outputs?
                .SingleOrDefault(o => o.Type == OutputType.Live);
            Options = settingsService.Contents.Options;

            eventAggregator?.GetEvent<SettingsUpdatedEvent>().Subscribe(
                action: UpdateAttributes,
                keepSubscriberReferenceAlive: true);

            UpdateDevices();
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

        public bool IsActive => sessionService.Contents.SettingsVisible;

        public Options Options { get; }

        public DelegateCommand UpdateDevicesCommand { get; }

        public float Volume
        {
            get { return outputSettings.Volume; }
            set
            {
                if (value != outputService.Volume)
                {
                    outputService.Volume = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Volume));
                }
            }
        }

        public int VolumePercentageApps
        {
            get { return outputSettings.VolumePercentageApps; }
            set
            {
                if (value != outputSettings.VolumePercentageApps)
                {
                    outputSettings.VolumePercentageApps = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(VolumePercentageApps));
                }
            }
        }

        public int VolumePercentageBackground
        {
            get { return outputSettings.VolumePercentageBackground; }
            set
            {
                if (value != outputSettings.VolumePercentageBackground)
                {
                    outputSettings.VolumePercentageBackground = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(VolumePercentageBackground));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void UpdateAttributes()
        {
            RaisePropertyChanged(nameof(IsActive));
        }

        private void UpdateDevices()
        {
            Device = outputService.DeviceName;

            isUpdate = true;

            Devices = new ObservableCollection<string>(outputService.GetDevices()
                .Select(d => d.Value));

            isUpdate = false;
        }

        #endregion Private Methods
    }
}