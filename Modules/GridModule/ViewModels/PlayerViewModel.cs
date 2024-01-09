using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SharpHook;
using SharpHook.Native;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel, IDisposable
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly TaskPoolGlobalHook hook;
        private readonly HashSet<ISignalService> signalServices = new();
        private readonly ITrackService trackService;

        private Bitmap bitmap;
        private string content;
        private string counter;
        private bool isDisposed;
        private bool isEnding;
        private KeyCode? key;
        private IMixerService mixerServiceBackground;
        private IMixerService mixerServiceForeground;
        private IMixerService mixerServicePFL;
        private string name;
        private bool onForeground;
        private string remaining;
        private ISignalService signalService;
        private IStackService stackService;
        private long widthPosition;
        private long widthRemaining;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            ITrackService trackService, TaskPoolGlobalHook hook)
        {
            this.containerProvider = containerProvider;
            this.trackService = trackService;
            this.hook = hook;

            if (hook != default)
            {
                hook.KeyPressed += OnKeyPressed;
            }

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecuteSignal,
                canExecuteMethod: () => IsExecuteAvailable());

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => UpdateStack(true),
                canExecuteMethod: () => IsNextTrackAvailable());

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => UpdateStack(false),
                canExecuteMethod: () => IsNextTrackAvailable());

            eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
                action: s => OnSignalStarted(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: s => OnSignalStopped(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateRemaining(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => UpdateStack(false),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s
                    && !OnForeground);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => stackService?.Track == t);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { SetProperty(ref bitmap, value); }
        }

        public string Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public string Counter
        {
            get { return counter; }
            set { SetProperty(ref counter, value); }
        }

        public DelegateCommand ExecuteCommand { get; }

        public bool HasKey => Key.HasValue;

        public bool HasStack => stackService?.Entries?.Count > 1;

        public virtual bool IsActive => signalService?.IsPlaying == true;

        public bool IsEnding
        {
            get { return isEnding; }
            set { SetProperty(ref isEnding, value); }
        }

        public virtual bool IsPlaying => signalServices?.Any() == true;

        public bool IsVisible { get; set; }

        public KeyCode? Key
        {
            get { return key; }
            set { SetProperty(ref key, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public bool OnForeground
        {
            get { return onForeground; }
            set { SetProperty(ref onForeground, value); }
        }

        public string Remaining
        {
            get { return remaining; }
            set { SetProperty(ref remaining, value); }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

        public long WidthPosition
        {
            get { return widthPosition; }
            set { SetProperty(ref widthPosition, value); }
        }

        public long WidthRemaining
        {
            get { return widthRemaining; }
            set { SetProperty(ref widthRemaining, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Initialize(Player player)
        {
            base.Initialize(
                cell: player);

            Name = player.Name;
            Key = player.Key;
            OnForeground = player.OnForeground;

            stackService = containerProvider?.Resolve<IStackService>();

            mixerServiceBackground = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));
            mixerServiceForeground = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Foreground));
            mixerServicePFL = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.PFL));

            stackService?.Initialize(
                path: player.Path,
                playerName: player.Name,
                savePosition: player.SavePositions);

            UpdateSignal();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    hook.KeyPressed -= OnKeyPressed;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void ExecuteSignal()
        {
            if (signalService != default)
            {
                if (signalService.State == StateType.Play)
                {
                    signalService.Pause();
                }
                else
                {
                    signalService.Start();
                }
            }
        }

        private IMixerService GetMixerService()
        {
            var result = OnForeground
                ? mixerServiceForeground
                : mixerServiceBackground;

            return result;
        }

        private bool IsExecuteAvailable()
        {
            var result = stackService?.Track != default;

            return result;
        }

        private bool IsNextTrackAvailable()
        {
            var result = stackService?.HasUnplayeds == true;

            return result;
        }

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (IsVisible
                && Key.HasValue
                && e?.Data.KeyCode == Key)
            {
                ExecuteSignal();
            }
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            if (this.signalService.Track == this.stackService?.Track)
            {
                this.stackService.Play();
            }

            signalServices.Add(signalService);

            UpdateCommands();
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if ((signalService == this.signalService)
                && (signalService.State == StateType.None || OnForeground || signalService.IsNextThresholdExceeded))
            {
                signalService.Dispose();

                UpdateStack();
            }

            signalServices.Remove(signalService);

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(IsPlaying));
            RaisePropertyChanged(nameof(HasStack));

            ExecuteCommand.RaiseCanExecuteChanged();
            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateRemaining()
        {
            Remaining = signalService?.Track == stackService?.Track
                ? signalService?.Remaining?.ToString(Constants.FormatDuration)
                : stackService?.Track?.Length.ToString(Constants.FormatDuration);

            IsEnding = signalService?.IsEnding == true;

            if (signalService?.Duration?.Ticks > 0)
            {
                WidthPosition = signalService.Position.Value.Ticks;
                WidthRemaining = signalService.Position.Value.Ticks;
            }
        }

        private void UpdateSignal()
        {
            if (stackService.Track != default)
            {
                var mixerService = GetMixerService();
                signalService = mixerService?.Add(stackService.Track);

                UpdateTrack();
            }
        }

        private void UpdateStack(bool toPrevious = false)
        {
            if (toPrevious)
            {
                stackService.Previous();
            }
            else
            {
                stackService.Next();
            }

            UpdateSignal();
        }

        private void UpdateTrack()
        {
            if (stackService.Track != default)
            {
                Content = stackService.Track.Info;
                Bitmap = stackService.Track.Bitmap;

                var count = trackService.GetCount(stackService.Track);
                Counter = $"[{count}]";

                trackService?.Render(stackService.Track);

                UpdateRemaining();
            }

            UpdateCommands();
        }

        #endregion Private Methods
    }
}