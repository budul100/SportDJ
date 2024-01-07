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

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel, IDisposable
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly TaskPoolGlobalHook hook;
        private readonly ITrackService trackService;

        private string artist;
        private string counter;
        private bool isDisposed;
        private KeyCode? key;
        private IMixerService mixerServiceBackground;
        private IMixerService mixerServiceForeground;
        private IMixerService mixerServicePFL;
        private string name;
        private bool onForeground;
        private string remaining;
        private ISignalService signalService;
        private IStackService stackService;
        private string title;

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
                action: _ => OnSignalStarted(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: _ => OnSignalStopped(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateRemaining(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => UpdateStack(false),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

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

        public string Artist
        {
            get { return artist; }
            set { SetProperty(ref artist, value); }
        }

        public string Counter
        {
            get { return counter; }
            set { SetProperty(ref counter, value); }
        }

        public DelegateCommand ExecuteCommand { get; }

        public bool HasKey => Key.HasValue;

        public bool HasStack => stackService?.Entries?.Count > 1;

        public bool IsPlaying => signalService?.IsPlaying == true;

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

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

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
                controllerName: player.Name,
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
            if (Key.HasValue
                && e?.Data.KeyCode == Key)
            {
                ExecuteSignal();
            }
        }

        private void OnSignalStarted()
        {
            if (this.signalService.Track == this.stackService?.Track)
            {
                this.stackService.Play();
            }

            UpdateCommands();
        }

        private void OnSignalStopped()
        {
            if (signalService.State == StateType.None || signalService.IsNextThresholdExceeded)
            {
                signalService.Dispose();

                UpdateStack();
            }

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            RaisePropertyChanged(nameof(IsPlaying));
            RaisePropertyChanged(nameof(HasStack));

            ExecuteCommand.RaiseCanExecuteChanged();
            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateRemaining()
        {
            Remaining = this.signalService?.Track == this.stackService?.Track
                ? signalService?.Remaining.ToString(Constants.FormatDuration)
                : stackService?.Track?.Length.ToString(Constants.FormatDuration);
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
                Title = stackService.Track.Title;
                Artist = stackService.Track.Artist;

                var count = trackService.GetCount(stackService.Track);
                Counter = $"[{count}]";

                UpdateRemaining();
            }

            UpdateCommands();
        }

        #endregion Private Methods
    }
}