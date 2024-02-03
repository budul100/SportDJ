using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SportDJ.Services.SignalService
{
    public class Service
        : ISignalService
    {
        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Set> settingsService;
        private readonly ITrackService trackService;

        private bool isDisposed;
        private TimeSpan? nextThresholdOnPlay;
        private FadedControlProvider outputProvider;
        private PlayingFileProvider playingProvider;
        private bool selectNextAlways;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
            ITrackService trackService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.trackService = trackService;

            cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan? Duration => !(playingProvider?.Duration < TimeSpan.Zero)
            ? playingProvider?.Duration
            : TimeSpan.Zero;

        public bool IsEnding => playingProvider != default
            && (playingProvider.PlayType == PlayType.Play || playingProvider.PlayType == PlayType.Stop)
            && playingProvider.Remaining <= TimeSpan.FromSeconds(Constants.IsEndingSeconds);

        public bool IsLoop
        {
            get
            {
                return playingProvider?.IsLoop ?? default;
            }
            set
            {
                if (playingProvider != default)
                {
                    playingProvider.IsLoop = value;
                }
            }
        }

        public bool IsOverNextThreshold { get; private set; }

        public bool IsPlaying => playingProvider?.PlayType == PlayType.Play;

        public bool IsSilent => outputProvider?.IsSilent != false;

        public ISampleProvider Output => outputProvider;

        public TimeSpan? Position => !(playingProvider?.Position < TimeSpan.Zero)
            ? playingProvider?.Position
            : TimeSpan.Zero;

        public double? Progress => Duration?.Ticks > 0
            ? (Position?.Ticks ?? 0) / (double)Duration.Value.Ticks
            : default;

        public TimeSpan? Remaining => !(playingProvider?.Remaining < TimeSpan.Zero)
            ? playingProvider?.Remaining
            : TimeSpan.Zero;

        public StateType State { get; private set; }

        public Track Track { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(Track track, WaveFormat waveFormat)
        {
            if (track == default)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (File.Exists(track.Path)
                && State == StateType.None)
            {
                Track = track;

                var sourceProvider = new AudioFileReader(Track.Path);

                playingProvider = new PlayingFileProvider(
                    sourceProvider: sourceProvider,
                    track: Track);

                playingProvider.SignalPausedEvent += OnSignalPaused;
                playingProvider.SignalStoppedEvent += OnSignalStopped;
                playingProvider.SignalEndedEvent += OnSignalEnded;
                playingProvider.SignalUpdatedEvent += OnSignalUpdated;

                var channelProvider = default(ISampleProvider);

                if (playingProvider.WaveFormat.Channels == waveFormat.Channels)
                {
                    channelProvider = playingProvider;
                }
                else
                {
                    channelProvider = playingProvider.WaveFormat.Channels == 1
                        ? new MonoToStereoSampleProvider(playingProvider)
                        : new StereoToMonoSampleProvider(playingProvider);
                }

                var resampleProvider = default(ISampleProvider);

                if (channelProvider.WaveFormat.SampleRate == waveFormat.SampleRate)
                {
                    resampleProvider = channelProvider;
                }
                else
                {
                    resampleProvider = new WdlResamplingSampleProvider(
                        source: channelProvider,
                        newSampleRate: waveFormat.SampleRate);
                }

                outputProvider = new FadedControlProvider(resampleProvider);

                outputProvider.FadingStartedEvent += OnFadingStarted;
                outputProvider.FadingEndedEvent += OnFadingEnded;

                if (settingsService.Contents.Options.SelectNextOnPlaySeconds.HasValue)
                {
                    var selectNextDuration = TimeSpan.FromSeconds(settingsService.Contents.Options.SelectNextOnPlaySeconds.Value);
                    nextThresholdOnPlay = playingProvider.Position.Add(selectNextDuration);

                    selectNextAlways = selectNextDuration > Track.Length;
                }
            }
        }

        public void Pause()
        {
            if (playingProvider != default
                && State == StateType.Play)
            {
                var milliSeconds = settingsService.Contents.Options.FadeOnPauseMilliseconds;

                outputProvider.Fade(
                    volumeTarget: Constants.VolumeMin,
                    milliSeconds: milliSeconds);
            }
        }

        public void Start()
        {
            if (playingProvider != default
                && (State == StateType.None || State == StateType.Pause))
            {
                var milliSeconds = State == StateType.Pause
                    ? settingsService.Contents.Options.FadeOnPauseMilliseconds
                    : 0;

                playingProvider.Start();

                outputProvider.Fade(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: milliSeconds);
            }
        }

        public void Stop()
        {
            State = StateType.None;

            playingProvider?.End();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    cancellationTokenSource?.Cancel();

                    if (playingProvider != default)
                    {
                        outputProvider.FadingStartedEvent -= OnFadingStarted;
                        outputProvider.FadingEndedEvent -= OnFadingEnded;

                        outputProvider.Dispose();

                        playingProvider.SignalPausedEvent -= OnSignalPaused;
                        playingProvider.SignalStoppedEvent -= OnSignalStopped;
                        playingProvider.SignalEndedEvent -= OnSignalEnded;
                        playingProvider.SignalUpdatedEvent -= OnSignalUpdated;

                        playingProvider.Dispose();
                    }
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnFadingEnded(object sender, EventArgs e)
        {
            if (outputProvider?.IsSilent == true)
            {
                playingProvider.Pause();
            }
        }

        private void OnFadingStarted(object sender, EventArgs e)
        {
            if (outputProvider?.IsSilent == true)
            {
                State = StateType.Play;

                trackService.Begin(Track);

                eventAggregator.GetEvent<SignalStartedEvent>().Publish(this);
            }
            else
            {
                State = StateType.Pause;
            }
        }

        private void OnSignalEnded(object sender, EventArgs e)
        {
            State = StateType.None;

            eventAggregator.GetEvent<SignalEndedEvent>().Publish(this);
        }

        private void OnSignalPaused(object sender, EventArgs e)
        {
            trackService?.End(Track);

            if (IsOverNextThreshold || selectNextAlways)
            {
                Stop();
            }
            else
            {
                eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);

                if (settingsService.Contents.Options.SelectNextOnPauseSeconds.HasValue)
                {
                    var selectNextDelay = TimeSpan.FromSeconds(settingsService.Contents.Options.SelectNextOnPauseSeconds.Value);

                    Task.Delay(
                        delay: selectNextDelay,
                        cancellationToken: cancellationTokenSource.Token)
                        .ContinueWith(_ => SelectNext());
                }
            }
        }

        private void OnSignalStopped(object sender, EventArgs e)
        {
            trackService?.End(Track);

            State = StateType.Stop;

            eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);
        }

        private void OnSignalUpdated(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);

            if (!IsOverNextThreshold
                && nextThresholdOnPlay.HasValue
                && Position?.Ticks > nextThresholdOnPlay.Value.Ticks)
            {
                IsOverNextThreshold = true;

                SelectNext();
            }
        }

        private void SelectNext()
        {
            eventAggregator.GetEvent<SignalNextEvent>().Publish(this);
        }

        #endregion Private Methods
    }
}