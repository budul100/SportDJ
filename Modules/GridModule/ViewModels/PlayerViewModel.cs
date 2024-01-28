using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SharpHook;
using SharpHook.Native;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel, IDisposable
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly TaskPoolGlobalHook hook;
        private readonly IMixerService mixerServiceBackground;
        private readonly IMixerService mixerServiceForeground;
        private readonly IMixerService mixerServicePFL;
        private readonly bool savePositions;
        private readonly HashSet<ISignalService> signalServices = new();
        private readonly ITrackService trackService;

        private Bitmap bitmap;
        private string content;
        private string counter;
        private bool isDisposed;
        private KeyCode? key;
        private string name;
        private bool onForeground;
        private string remaining;
        private ISignalService signalService;
        private IStackService stackService;
        private long widthPosition;
        private long widthRemaining;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, Player player)
            : base(player)
        {
            this.containerProvider = containerProvider;

            Name = player.Name;
            OnForeground = player.OnForeground;

            savePositions = player.SavePositions;

            mixerServiceBackground = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Background));
            mixerServiceForeground = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.Foreground));
            mixerServicePFL = containerProvider?.Resolve<IMixerService>(
                name: nameof(MixerType.PFL));

            trackService = containerProvider.Resolve<ITrackService>();

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecuteSignal,
                canExecuteMethod: () => IsExecuteAvailable());

            ResetCommand = new DelegateCommand(
                executeMethod: ResetStack,
                canExecuteMethod: () => IsResetAvailable());

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => SelectTrack(true),
                canExecuteMethod: () => IsNextTrackAvailable());

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => SelectTrack(false),
                canExecuteMethod: () => IsNextTrackAvailable());

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
                action: s => OnSignalStarted(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateRemaining(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: s => OnSignalStopped(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator?.GetEvent<SignalEndedEvent>().Subscribe(
                action: s => OnSignalEnded(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => SelectTrack(false),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s
                    && !OnForeground);

            eventAggregator?.GetEvent<StackDefinedEvent>().Subscribe(
                action: _ => UpdateTracks(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => stackService == s);

            eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
                action: _ => UpdateCommands(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => stackService == s);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: _ => UpdateSignal(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => stackService == s);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => stackService?.Track == t);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateOutput(),
                keepSubscriberReferenceAlive: true);

            hook = containerProvider.Resolve<TaskPoolGlobalHook>();

            if (hook != default
                && !string.IsNullOrWhiteSpace(player.Key))
            {
                Key = Enum.Parse<KeyCode>(player.Key);
                hook.KeyPressed += OnKeyPressed;
            }

            UpdateOutput();

            UpdateStack(
                path: player.Path);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler ClosingContextMenuEvent;

        #endregion Public Events

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

        public bool HasPFL { get; private set; }

        public bool HasPFLOrKey => HasPFL || HasKey;

        public bool HasStack => stackService?.Entries?.Count > 1;

        public virtual bool IsActive => signalService?.IsPlaying == true;

        public bool IsEnding => signalService?.IsEnding == true;

        public virtual bool IsPlaying => signalServices?.Any(s => s.State != StateType.None) == true;

        public KeyCode? Key
        {
            get { return key; }
            set
            {
                SetProperty(ref key, value);

                RaisePropertyChanged(nameof(HasKey));
            }
        }

        public ObservableCollection<MenuViewModel> Menus { get; private set; }

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

        public DelegateCommand ResetCommand { get; }

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

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing
                    && hook != default)
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
                else if (signalService.State != StateType.Stop)
                {
                    signalService.Start();
                }
            }
        }

        private IEnumerable<MenuViewModel> GetMenus()
        {
            if (stackService?.Entries?.Any() == true)
            {
                for (var index = 0; index < stackService.Entries.Count; index++)
                {
                    var current = index;

                    var result = new MenuViewModel(
                        containerProvider: containerProvider,
                        entry: stackService.Entries[index],
                        callback: () => stackService.Select(current));

                    result.ClosingEvent += OnClosingContextMenu;

                    yield return result;
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

        private bool IsResetAvailable()
        {
            var result = stackService?.HasPlayeds == true;

            return result;
        }

        private void OnClosingContextMenu(object sender, EventArgs e)
        {
            ClosingContextMenuEvent?.Invoke(
                sender: this,
                e: default);
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

        private void OnSignalEnded(ISignalService signalService)
        {
            if (signalService.State == StateType.None)
            {
                signalService.Dispose();

                signalServices.Remove(signalService);
            }

            if (this.signalService == signalService
                && stackService?.Track == this.signalService.Track)
            {
                SelectTrack();
            }
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                if (signalService.Track == stackService?.Track)
                {
                    stackService.Play();
                }

                signalServices.Add(signalService);
            }

            UpdateCommands();
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if ((signalService == this.signalService)
                && (signalService.State == StateType.None || OnForeground || signalService.IsOverNextThreshold))
            {
                signalService.Stop();
            }

            UpdateCommands();
        }

        private void ResetStack()
        {
            stackService?.Reset();
        }

        private void SelectTrack(bool toPrevious = false)
        {
            if (toPrevious)
            {
                stackService.Previous();
            }
            else
            {
                stackService.Next();
            }
        }

        private void UpdateCommands()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(IsPlaying));
            RaisePropertyChanged(nameof(IsEnding));

            RaisePropertyChanged(nameof(HasKey));
            RaisePropertyChanged(nameof(HasPFL));
            RaisePropertyChanged(nameof(HasPFLOrKey));
            RaisePropertyChanged(nameof(HasStack));

            ExecuteCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();

            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateOutput()
        {
            var outputPFL = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.PFL));

            HasPFL = outputPFL?.IsActive == true;

            UpdateCommands();
        }

        private void UpdateRemaining()
        {
            Remaining = signalService?.Track == stackService?.Track
                ? signalService?.Remaining?.ToString(Constants.FormatDuration)
                : stackService?.Track?.Length.ToString(Constants.FormatDuration);

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
                if (this.signalService?.State == StateType.Pause)
                {
                    this.signalService?.Stop();
                }

                var mixerService = GetMixerService();
                this.signalService = mixerService?.Add(stackService.Track);

                UpdateTrack();
            }
        }

        private void UpdateStack(string path)
        {
            stackService = containerProvider?.Resolve<IStackService>();

            Task.Run(() => stackService?.Initialize(
                path: path,
                name: Name,
                savePosition: savePositions));
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

        private void UpdateTracks()
        {
            if (stackService?.Entries?.Any() == true)
            {
                Menus = new ObservableCollection<MenuViewModel>(GetMenus().ToArray());
            }
        }

        #endregion Private Methods
    }
}