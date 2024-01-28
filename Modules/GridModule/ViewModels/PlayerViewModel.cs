using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IMixerService mixerServiceBackground;
        private readonly IMixerService mixerServiceForeground;
        private readonly IMixerService mixerServicePFL;
        private readonly HashSet<ISignalService> signalServices = new();
        private readonly ITrackService trackService;

        private Bitmap bitmap;
        private string content;
        private string counter;
        private string keyCode;
        private string name;
        private bool onForeground;
        private string remaining;
        private bool savePositions;
        private ISignalService signalService;
        private IStackService stackService;
        private long widthPosition;
        private long widthRemaining;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, Player player, GridViewModel parent)
            : base(player)
        {
            this.containerProvider = containerProvider;

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
                filter: s => !OnForeground
                    && signalService == s);

            eventAggregator?.GetEvent<StackDefinedEvent>().Subscribe(
                action: _ => UpdateTracks(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => stackService == s);

            eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
                action: _ => UpdateContent(),
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

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: _ => ExecuteSignal(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: k => parent.IsVisible
                    && k == player.KeyCode);

            Intialize(
                player: player);
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

        public bool HasKey => !string.IsNullOrWhiteSpace(Key);

        public bool HasPFL { get; private set; }

        public bool HasPFLOrKey => HasPFL || HasKey;

        public bool HasStack => stackService?.Entries?.Count > 1;

        public virtual bool IsActive => signalService?.IsPlaying == true;

        public bool IsEnding => signalService?.IsEnding == true;

        public virtual bool IsPlaying => signalServices?.Any(s => s.State != StateType.None) == true;

        public string Key => KeyCode.GetKey();

        public string KeyCode
        {
            get { return keyCode; }
            set
            {
                if (value != keyCode)
                {
                    keyCode = value;

                    RaisePropertyChanged(nameof(Key));
                    RaisePropertyChanged(nameof(HasKey));
                }
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

        private void Intialize(Player player)
        {
            Name = player.Name;
            KeyCode = player.KeyCode;
            OnForeground = player.OnForeground;
            savePositions = player.SavePositions;

            stackService = containerProvider?.Resolve<IStackService>();

            Task.Run(() => stackService?.Initialize(
                path: player.Path,
                name: Name,
                savePosition: savePositions));

            UpdateOutput();
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

        private void OnSignalEnded(ISignalService signalService)
        {
            if (this.signalService == signalService
                && stackService?.Track == this.signalService.Track)
            {
                SelectTrack();
            }

            if (signalService.State == StateType.None)
            {
                signalService.Dispose();

                signalServices.Remove(signalService);
            }

            UpdateContent();
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

            UpdateContent();
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if ((signalService == this.signalService)
                && (signalService.State == StateType.None || OnForeground || signalService.IsOverNextThreshold))
            {
                signalService.Stop();
            }

            UpdateContent();
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

        private void UpdateContent()
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

            UpdateContent();
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

            UpdateContent();
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