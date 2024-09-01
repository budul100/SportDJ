using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SportDJ.Services.PlayerService
{
    public class Service
        : IPlayerService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Set> settingsService;
        private readonly HashSet<ISignalService> signalServices = [];
        private readonly IStackService stackService;
        private readonly ITrackService trackService;

        private bool isDisposed;
        private MixerType mixerType;
        private Player player;
        private ISignalService signalService;
        private IStorageFolder startLocation;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStackService stackService, ITrackService trackService, IDialogService dialogService,
            IStorageService<Set> settingsService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.trackService = trackService;
            this.dialogService = dialogService;
            this.settingsService = settingsService;
            this.stackService = stackService;

            eventAggregator?.GetEvent<AppStartedEvent>().Subscribe(
                action: SetStartLocation,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateDuration(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalStartedEvent>().Subscribe(
                action: OnSignalStarted,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

            eventAggregator?.GetEvent<SignalStoppedEvent>().Subscribe(
                action: OnSignalStopped,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: signalServices.Contains);

            eventAggregator?.GetEvent<SignalEndedEvent>().Subscribe(
                action: OnSignalEnded,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == signalService || signalServices.Contains(s));

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => Next(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s
                    && mixerType == MixerType.Background
                    && !IsLoop);

            eventAggregator?.GetEvent<StackDefinedEvent>().Subscribe(
                action: _ => eventAggregator.GetEvent<PlayerInitializedEvent>().Publish(this),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => this.stackService == s);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: _ => UpdateSignal(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => this.stackService == s);

            eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
                action: _ => UpdatePlayer(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => this.stackService == s);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => this.stackService.Track == t);
        }

        #endregion Public Constructors

        #region Public Properties

        public int Counter { get; private set; }

        public IEnumerable<Entry> Entries => stackService.Entries;

        public bool HasPaths => player?.Paths?.Count > 0;

        public bool HasPlayeds => stackService.HasPlayeds;

        public bool HasStack => Entries?.Count() > 1;

        public bool HasUnplayeds => stackService.HasUnplayeds;

        public bool IsActive => signalServices?.Any(s => s.State != StateType.None) == true;

        public bool IsAvailable => signalService?.Track != default;

        public bool IsEnding => signalService?.IsEnding == true;

        public bool IsForeground
        {
            get => player?.IsForeground == true;

            set
            {
                if (player != default
                    && player.IsForeground != value
                    && mixerType != MixerType.PFL)
                {
                    player.IsForeground = value;

                    settingsService.Save();

                    UpdateSignal();
                }
            }
        }

        public bool IsLoop
        {
            get => player?.IsLoop == true;

            set
            {
                if (player != default
                    && player.IsLoop != value)
                {
                    player.IsLoop = value;

                    settingsService.Save();

                    if (signalService != default)
                    {
                        signalService.IsLoop = IsLoop;
                    }
                }
            }
        }

        public bool IsPlaying => signalService?.IsPlaying == true;

        public double? Progress { get; private set; }

        public TimeSpan? Remaining { get; private set; }

        public bool SavePosition
        {
            get => player?.SavePosition == true;

            set
            {
                if (player != default
                    && player.SavePosition != value)
                {
                    player.SavePosition = value;

                    settingsService.Save();
                }
            }
        }

        public Track Track => stackService.Track;

        #endregion Public Properties

        #region Public Methods

        public async Task ClearAsync()
        {
            if (HasPaths)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.PlayerResetMessage,
                    contentTitle: Texts.PlayerResetHeader,
                    buttonDefinitions: ButtonEnum.YesNo,
                    enterDefaultButton: ClickEnum.No);

                if (result == ButtonResult.Yes)
                {
                    player.Paths.Clear();

                    settingsService.Save();

                    InitializePlayer();
                }
            }
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Execute()
        {
            if (IsAvailable)
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

        public void Initialize(Player player)
        {
            this.player = player;

            InitializePlayer();
        }

        public void Next()
        {
            stackService.Next();
        }

        public void Previous()
        {
            stackService.Previous();
        }

        public void Reset()
        {
            stackService.Reset();
        }

        public async Task SetFileAsync()
        {
            var paths = await dialogService.OpenFilePickerAsync(
                title: Texts.PlayerSelectionFileHeader,
                allowMultiple: true,
                startLocation: startLocation);

            if (paths?.Any() == true)
            {
                player.Paths = paths
                    .Select(p => p.Path.LocalPath)
                    .Where(File.Exists).ToList();

                settingsService.Save();

                InitializePlayer();
            }
        }

        public async Task SetFolderAsync()
        {
            var paths = await dialogService.OpenFolderPickerAsync(
                title: Texts.PlayerSelectionFolderHeader,
                allowMultiple: true,
                startLocation: startLocation);

            if (paths?.Any() == true)
            {
                player.Paths = paths
                    .Select(p => p.Path.LocalPath)
                    .Where(Directory.Exists).ToList();

                settingsService.Save();

                InitializePlayer();
            }
        }

        public async Task ShuffleAsync()
        {
            if (HasStack)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.PlayerShuffleMessage,
                    contentTitle: Texts.PlayerShuffleHeader,
                    buttonDefinitions: ButtonEnum.YesNo,
                    enterDefaultButton: ClickEnum.No);

                if (result == ButtonResult.Yes)
                {
                    stackService.Shuffle();
                }
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    signalService?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void InitializePlayer()
        {
            Counter = 0;

            SetStartLocation();

            Task.Run(() => stackService.Initialize(
                paths: player.Paths,
                name: player.Name,
                savePosition: SavePosition,
                isLoop: IsLoop));
        }

        private void OnSignalEnded(ISignalService signalService)
        {
            if (!signalService.IsLoop)
            {
                if (this.signalService == signalService
                    && signalService.Track == stackService.Track)
                {
                    Next();
                }

                if (signalService.IsSilent)
                {
                    signalServices.Remove(signalService);

                    signalService.Dispose();
                }
            }

            UpdatePlayer();
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                if (signalService.Track == stackService.Track)
                {
                    stackService.Play();
                }

                signalServices.Add(signalService);
            }

            UpdatePlayer();
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if ((signalService == this.signalService)
                && (signalService.State == StateType.None
                || signalService.IsOverNextThreshold
                || IsForeground))
            {
                signalService.Stop();
            }

            UpdatePlayer();
        }

        private void SetStartLocation()
        {
            Task.Run(async () => startLocation = await dialogService.GetFolderAsync(player.Paths));
        }

        private void UpdateDuration()
        {
            var remaining = signalService?.Track == stackService.Track
                ? signalService?.Remaining
                : stackService?.Track?.Duration;

            if (remaining?.Ticks > 0)
            {
                Remaining = remaining.Value;
                Progress = signalService?.Track == stackService.Track
                    ? signalService.Progress
                    : default;
            }
            else
            {
                Remaining = default;
                Progress = default;
            }

            eventAggregator.GetEvent<PlayerShiftedEvent>().Publish(this);
        }

        private void UpdatePlayer()
        {
            eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(this);
        }

        private void UpdateSignal()
        {
            if (stackService.Track != default)
            {
                mixerType = player.IsForeground
                    ? MixerType.Foreground
                    : MixerType.Background;

                var mixerService = containerProvider?.Resolve<IMixerService>(
                    name: mixerType.ToString());

                signalService = mixerService?.Add(
                    track: stackService.Track,
                    isLoop: IsLoop);
            }

            UpdateTrack();
        }

        private void UpdateTrack()
        {
            if (stackService.Track != default)
            {
                Counter = trackService.GetCount(stackService.Track);
                trackService?.Render(stackService.Track);
            }
            else
            {
                Counter = default;
            }

            UpdatePlayer();
            UpdateDuration();
        }

        #endregion Private Methods
    }
}