using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using SharpHook;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Prism;
using System;

namespace SportDJ.Modules.ControllerModule.ViewModels
{
    public class ControllerViewModel
        : RegionViewModelBase, IDisposable
    {
        #region Private Fields

        private readonly TaskPoolGlobalHook hook;
        private readonly Options options;
        private readonly IOutputService outputLiveService;
        private readonly IVolumeService volumeService;

        private bool isDisposed;
        private bool othersMuted;
        private bool volumeMonitoringActive;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IStorageService<Set> settingsService, TaskPoolGlobalHook hook)
            : base(regionManager)
        {
            this.hook = hook;
            this.options = settingsService.Contents.Options;

            hook.KeyPressed += OnKeyPressed;
            hook.KeyReleased += OnKeyReleased;

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

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateCommands(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateCommands(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputLiveService == o);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive => outputLiveService.IsPlaying || VolumeMonitoringActive;

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

                UpdateCommands();
            }
        }

        public DelegateCommand VolumeRaiseCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    hook.KeyPressed += OnKeyPressed;
                    hook.KeyReleased += OnKeyReleased;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (options.KeyPauseOutput.HasValue
                && e?.Data.KeyCode == options.KeyPauseOutput)
            {
                outputLiveService.Pause();
            }
            else if (options.KeyTalkVolume.HasValue
                && e?.Data.KeyCode == options.KeyTalkVolume)
            {
                outputLiveService.Lower();
            }
        }

        private void OnKeyReleased(object sender, KeyboardHookEventArgs e)
        {
            if (options.KeyTalkVolume.HasValue
                && e?.Data.KeyCode == options.KeyTalkVolume)
            {
                outputLiveService.Raise();
            }
        }

        private void UpdateCommands()
        {
            RaisePropertyChanged(nameof(IsActive));

            PauseCommand.RaiseCanExecuteChanged();
            VolumeLowerCommand.RaiseCanExecuteChanged();
            VolumeRaiseCommand.RaiseCanExecuteChanged();
        }

        #endregion Private Methods
    }
}