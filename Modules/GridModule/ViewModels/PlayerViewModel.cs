using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IStorageService<Set> settingsService;
        private readonly HashSet<ISignalService> signalServices = new();
        private readonly ITrackService trackService;

        private Bitmap bitmap;
        private string content;
        private string counter;
        private ObservableCollection<BindableBase> menus;
        private IOutputService outputServiceLive;
        private Player player;
        private string remaining;
        private ISignalService signalService;
        private IStackService stackService;

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

            settingsService = containerProvider.Resolve<IStorageService<Set>>();
            trackService = containerProvider.Resolve<ITrackService>();

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecutePlayer);

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => SelectTrack(true),
                canExecuteMethod: () => IsSelectionAvailable);

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => SelectTrack(false),
                canExecuteMethod: () => IsSelectionAvailable);

            ResetCommand = new DelegateCommand(
                executeMethod: ResetStack,
                canExecuteMethod: () => IsResetAvailable);

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
                action: s => OnSignalStarted(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateDuration(),
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
                action: _ => SelectTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => !IsForeground
                    && !IsLoop
                    && signalService == s);

            eventAggregator?.GetEvent<StackDefinedEvent>().Subscribe(
                action: _ => UpdateMenu(),
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

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputServiceLive == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputServiceLive == o);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateOutput(),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: _ => ExecutePlayer(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: k => parent.IsVisible
                    && k == player.KeyCode);

            Intialize(
                player: player);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        public event EventHandler MenuOpenEvent;

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

        public bool IsExecuteAvailable => stackService?.Track != default;

        public bool IsForeground
        {
            get { return player.IsForeground; }
            set
            {
                if (value != player.IsForeground)
                {
                    player.IsForeground = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(IsForeground));
                }
            }
        }

        public bool IsLoop
        {
            get { return player.IsLoop; }
            set
            {
                if (value != player.IsLoop)
                {
                    player.IsLoop = value;

                    settingsService.Save();

                    UpdateLoop();

                    RaisePropertyChanged(nameof(IsLoop));
                }
            }
        }

        public virtual bool IsPlaying => signalServices?.Any(s => s.State != StateType.None) == true;

        public bool IsResetAvailable => stackService?.HasPlayeds == true;

        public bool IsSelectionAvailable => stackService?.HasUnplayeds == true;

        public string Key => KeyCode.GetKey();

        public string KeyCode
        {
            get { return player.KeyCode; }
            set
            {
                if (value != player.KeyCode)
                {
                    player.KeyCode = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(KeyCode));
                    RaisePropertyChanged(nameof(HasKey));
                }
            }
        }

        public ObservableCollection<BindableBase> Menus => settingsService.Contents.Options.EnableInterruptingDialogs || !outputServiceLive.IsPlaying
            ? menus
            : default;

        public string Name
        {
            get { return player.Name; }
            set
            {
                if (value != player.Name)
                {
                    player.Name = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public string Remaining
        {
            get { return remaining; }
            set { SetProperty(ref remaining, value); }
        }

        public DelegateCommand ResetCommand { get; }

        public bool SavePositions
        {
            get { return player.SavePositions; }
            set
            {
                if (value != player.SavePositions)
                {
                    player.SavePositions = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(SavePositions));
                }
            }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

        public long? WidthPosition { get; protected set; }

        public long? WidthRemaining { get; protected set; }

        #endregion Public Properties

        #region Private Methods

        private void ExecutePlayer()
        {
            if (stackService?.Entries?.Any() == true
                && signalService != default)
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
            else
            {
                MenuOpenEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        private IEnumerable<BindableBase> GetMenuItems()
        {
            var openItem = new MenuViewModel(
                header: "Select Tracks ...",
                callback: default);

            yield return openItem;

            var menuItems = GetMenuTracks()
                .OrderBy(m => m.Header).ToArray();

            if (menuItems?.Length > 1)
            {
                yield return new SeparatorViewModel();

                var groupsSize = (menuItems.Length / ((menuItems.Length / Constants.MenuItemsSize) + 1)) + 1;

                var groups = menuItems
                    .Chunk(groupsSize).ToArray();

                foreach (var group in groups)
                {
                    var result = new MenuViewModel(
                        menuItems: group);

                    result.CloseMenuEvent += OnMenuClosing;

                    yield return result;
                }
            }
        }

        private IEnumerable<MenuViewModel> GetMenuTracks()
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

                    result.CloseMenuEvent += OnMenuClosing;

                    yield return result;
                }
            }
        }

        private IMixerService GetMixerService()
        {
            var result = IsForeground
                ? mixerServiceForeground
                : mixerServiceBackground;

            return result;
        }

        private void Intialize(Player player)
        {
            this.player = player;

            stackService = containerProvider?.Resolve<IStackService>();

            Task.Run(() => stackService?.Initialize(
                path: player.Path,
                name: player.Name,
                savePosition: player.SavePositions,
                isLoop: player.IsLoop));

            UpdateOutput();
            UpdateMenu();
        }

        private void OnMenuClosing(object sender, EventArgs e)
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void OnSignalEnded(ISignalService signalService)
        {
            if (!IsLoop)
            {
                if (this.signalService == signalService
                    && signalService.Track == stackService?.Track)
                {
                    SelectTrack();
                }

                if (signalService.State == StateType.None)
                {
                    signalService.Dispose();

                    signalServices.Remove(signalService);
                }
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
                && (signalService.State == StateType.None || IsForeground || signalService.IsOverNextThreshold))
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

            RaisePropertyChanged(nameof(Menus));

            ExecuteCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();

            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateDuration()
        {
            var length = signalService?.Track == stackService?.Track
                ? signalService?.Remaining
                : stackService?.Track?.Length;

            if (length?.Ticks > 0)
            {
                WidthPosition = signalService?.Track == stackService?.Track
                    && signalService?.Duration?.Ticks > 0
                    ? signalService.Position.Value.Ticks
                    : 0;

                WidthRemaining = length.Value.Ticks;

                Remaining = length.Value.ToString(Constants.FormatDuration);
            }
            else
            {
                WidthPosition = default;
                WidthRemaining = default;

                Remaining = default;
            }

            RaisePropertyChanged(nameof(WidthPosition));
            RaisePropertyChanged(nameof(WidthRemaining));
        }

        private void UpdateLoop()
        {
            if (signalService != default)
            {
                signalService.IsLoop = player.IsLoop;
            }
        }

        private void UpdateMenu()
        {
            menus = new ObservableCollection<BindableBase>(GetMenuItems());
        }

        private void UpdateOutput()
        {
            outputServiceLive = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.Live));

            var outputServicePFL = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.PFL));

            HasPFL = outputServicePFL?.IsActive == true;

            UpdateContent();
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
                UpdateLoop();
            }
        }

        private void UpdateTrack()
        {
            if (stackService.Track != default)
            {
                var count = trackService.GetCount(stackService.Track);
                Counter = $"[{count}]";

                Content = stackService.Track.Info;
                Bitmap = stackService.Track.Bitmap;

                UpdateDuration();

                trackService?.Render(stackService.Track);
            }

            UpdateContent();
        }

        #endregion Private Methods
    }
}