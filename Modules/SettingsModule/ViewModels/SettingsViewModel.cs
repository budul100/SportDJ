using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.SettingsModule.ViewModels
{
    public class SettingsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IOutputService outputService;
        private readonly Output outputSettings;
        private readonly IStorageService<Set> settingsService;

        private string device;
        private bool devicesNotAvailable;
        private bool isUpdate;

        #endregion Private Fields

        #region Public Constructors

        public SettingsViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IStorageService<Set> settingsService)
            : base(regionManager)
        {
            this.settingsService = settingsService;

            UpdateDevicesCommand = new DelegateCommand(
                executeMethod: () => UpdateDevices());

            outputService = containerProvider.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            outputSettings = settingsService.Contents.Outputs?
                .SingleOrDefault(o => o.Type == OutputType.Live);

            Options = settingsService.Contents.Options;

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

        public double VolumePortionApps
        {
            get { return outputSettings.VolumePortionApps; }
            set
            {
                if (value != outputSettings.VolumePortionApps)
                {
                    outputService.VolumePortionApps = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(VolumePortionApps));
                }
            }
        }

        public double VolumePortionBackground
        {
            get { return outputSettings.VolumePortionBackground; }
            set
            {
                if (value != outputService.VolumePortionBackground)
                {
                    outputService.VolumePortionApps = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(VolumePortionBackground));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

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