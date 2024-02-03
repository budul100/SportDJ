using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportDJ.Services.PlayerService
{
    public class Service
        : IPlayerService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly HashSet<ISignalService> signalServices = new();
        private readonly ITrackService trackService;

        private bool isLoop;
        private ISignalService signalService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStackService stackService, ITrackService trackService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.trackService = trackService;

            StackService = stackService;

            eventAggregator?.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => UpdateDuration(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s);

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

            eventAggregator?.GetEvent<SignalEndedEvent>().Subscribe(
                action: s => OnSignalEnded(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator?.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => Next(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalService == s
                    && MixerType == MixerType.Background
                    && !IsLoop);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: _ => UpdateSignal(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => StackService == s);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => Track == t);
        }

        #endregion Public Constructors

        #region Public Properties

        public int Counter { get; private set; }

        public bool HasPlayeds => StackService.HasPlayeds;

        public bool HasStack => StackService.Entries?.Count > 1;

        public bool HasUnplayeds => StackService.HasUnplayeds;

        public bool IsActive => signalServices?.Any(s => s.State != StateType.None) == true;

        public bool IsAvailable => signalService?.Track != default;

        public bool IsEnding => signalService?.IsEnding == true;

        public bool IsLoop
        {
            get => isLoop;
            set
            {
                if (signalService != default
                    && isLoop != value)
                {
                    isLoop = value;

                    if (signalService != default)
                    {
                        signalService.IsLoop = IsLoop;
                    }
                }
            }
        }

        public bool IsPlaying => signalService?.IsPlaying == true;

        public MixerType MixerType { get; set; }

        public double? Progress { get; private set; }

        public TimeSpan? Remaining { get; private set; }

        public IStackService StackService { get; }

        public Track Track => StackService.Track;

        #endregion Public Properties

        #region Public Methods

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

        public void Initialize(string path, string name, bool savePosition, bool isLoop, MixerType mixerType)
        {
            MixerType = mixerType;
            IsLoop = isLoop;
            Counter = 0;

            Task.Run(() => StackService.Initialize(
                path: path,
                name: name,
                savePosition: savePosition,
                isLoop: isLoop));
        }

        public void Next()
        {
            StackService.Next();
        }

        public void Previous()
        {
            StackService.Previous();
        }

        public void Reset()
        {
            StackService.Reset();
        }

        #endregion Public Methods

        #region Private Methods

        private void OnPlayerDefined()
        {
            eventAggregator.GetEvent<PlayerDefinedEvent>().Publish(this);
        }

        private void OnSignalEnded(ISignalService signalService)
        {
            if (!signalService.IsLoop)
            {
                if (this.signalService == signalService
                    && signalService.Track == StackService.Track)
                {
                    Next();
                }

                if (signalService.State == StateType.None)
                {
                    signalServices.Remove(signalService);

                    signalService.Dispose();
                }
            }

            OnPlayerDefined();
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            if (signalService == this.signalService)
            {
                if (signalService.Track == StackService.Track)
                {
                    StackService.Play();
                }

                signalServices.Add(signalService);
            }

            OnPlayerDefined();
        }

        private void OnSignalStopped(ISignalService signalService)
        {
            if ((signalService == this.signalService)
                && (signalService.State == StateType.None
                || signalService.IsOverNextThreshold
                || MixerType == MixerType.Foreground))
            {
                signalService.Stop();
            }

            OnPlayerDefined();
        }

        private void UpdateDuration()
        {
            var remaining = signalService?.Track == StackService.Track
                ? signalService?.Remaining
                : StackService?.Track?.Length;

            if (remaining?.Ticks > 0)
            {
                Remaining = remaining.Value;
                Progress = signalService?.Track == StackService.Track
                    ? signalService.Progress
                    : default;
            }
            else
            {
                Remaining = default;
                Progress = default;
            }

            eventAggregator.GetEvent<PlayerUpdatedEvent>().Publish(this);
        }

        private void UpdateSignal()
        {
            if (StackService.Track != default)
            {
                if (signalService?.State == StateType.Pause)
                {
                    signalService?.Stop();
                }

                var mixerService = containerProvider?.Resolve<IMixerService>(
                    name: MixerType.ToString());

                signalService = mixerService?.Add(
                    track: StackService.Track,
                    isLoop: IsLoop);

                UpdateTrack();
            }
        }

        private void UpdateTrack()
        {
            if (StackService.Track != default)
            {
                Counter = trackService.GetCount(StackService.Track);
                trackService?.Render(StackService.Track);
            }
            else
            {
                Counter = default;
            }

            OnPlayerDefined();

            UpdateDuration();
        }

        #endregion Private Methods
    }
}