using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.SignalService
{
    public class Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
        ITrackService trackService, IAudioSourceProvider sourceProvider)
        : ISignalService
    {
        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly long updatesInTicks = Constants.UpdatesInMilliSec * 10000;

        private DynamicFadingProvider fadingProvider;
        private bool isDisposed;
        private long lastPosition = 0;
        private TimeSpan? nextThresholdOnPlay;
        private SourcePlayingProvider playingProvider;

        #endregion Private Fields

        #region Public Properties

        public TimeSpan? Duration => !(playingProvider?.Duration < TimeSpan.Zero)
            ? playingProvider?.Duration
            : TimeSpan.Zero;

        public bool IsEnding => playingProvider?.IsRunning == true
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

        public bool IsPlaying => playingProvider?.PlayType == PlayType.Playing;

        public bool IsSilent => fadingProvider?.IsSilent != false;

        public ISampleProvider Output => fadingProvider;

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

            GC.SuppressFinalize(
                obj: this);
        }

        public void End()
        {
            State = StateType.None;

            playingProvider?.End();
        }

        public void Initialize(Track track, WaveFormat waveFormat)
        {
            ArgumentNullException.ThrowIfNull(track);

            if (File.Exists(track.Path)
                && State == StateType.None)
            {
                Track = track;

                var fadeOut = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0
                    ? TimeSpan.FromMilliseconds(settingsService.Contents.Options.FadeOnMixOutMilliseconds)
                    : TimeSpan.Zero;

                sourceProvider.Initialize(Track.Path);

                playingProvider = new SourcePlayingProvider(
                    sourceProvider: sourceProvider,
                    track: Track,
                    fadeOut: fadeOut);

                playingProvider.CueExceededEvent += OnCueExceeded;
                playingProvider.SignalPausedEvent += OnSignalPaused;
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

                fadingProvider = new DynamicFadingProvider(resampleProvider);

                fadingProvider.FadingStartedEvent += OnFadingStarted;
                fadingProvider.FadingEndedEvent += OnFadingEnded;

                if (settingsService.Contents.Options.SelectNextOnPlaySeconds.HasValue)
                {
                    var selectNextSeconds = settingsService.Contents.Options.SelectNextOnPlaySeconds.Value;
                    var selectNextDuration = TimeSpan.FromSeconds(selectNextSeconds);
                    nextThresholdOnPlay = playingProvider.Position.Add(selectNextDuration);
                }

                eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);
            }
        }

        public void Pause(bool useFilterEffect = false)
        {
            if (playingProvider != default
                && State == StateType.Play)
            {
                var fadeMilliSeconds = useFilterEffect
                    ? settingsService.Contents.Options.FadeOnPauseFilteredMilliseconds
                    : settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds;

                fadingProvider.FadeOut(
                    milliSeconds: fadeMilliSeconds,
                    useFilterEffect: useFilterEffect);
            }
        }

        public void Start()
        {
            if (playingProvider != default
                && (State == StateType.None || State == StateType.Pause))
            {
                var fadeMilliSeconds = State == StateType.Pause
                    ? settingsService.Contents.Options.FadeOnPauseUnfilteredMilliseconds
                    : 0;

                playingProvider.Start();

                fadingProvider.FadeIn(
                    milliSeconds: fadeMilliSeconds);
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
                    cancellationTokenSource?.Cancel();

                    if (playingProvider != default)
                    {
                        fadingProvider.FadingStartedEvent -= OnFadingStarted;
                        fadingProvider.FadingEndedEvent -= OnFadingEnded;

                        fadingProvider.Dispose();

                        playingProvider.CueExceededEvent -= OnCueExceeded;
                        playingProvider.SignalPausedEvent -= OnSignalPaused;
                        playingProvider.SignalEndedEvent -= OnSignalEnded;
                        playingProvider.SignalUpdatedEvent -= OnSignalUpdated;

                        playingProvider.Dispose();
                    }

                    Track = default;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnCueExceeded(object sender, EventArgs e)
        {
            trackService?.Stop(Track);

            State = StateType.Stop;

            eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);
        }

        private void OnFadingEnded(object sender, EventArgs e)
        {
            if (fadingProvider?.IsSilent == true)
            {
                playingProvider.Pause();
            }
        }

        private void OnFadingStarted(object sender, EventArgs e)
        {
            if (fadingProvider.FadeType == FadeType.Reduce)
            {
                State = StateType.Pause;
            }
            else if (State != StateType.Play)
            {
                var isPause = State == StateType.Pause;

                State = StateType.Play;

                trackService.Play(
                    track: Track,
                    isPause: isPause);

                eventAggregator.GetEvent<SignalStartedEvent>().Publish(this);
            }
        }

        private void OnSignalEnded(object sender, EventArgs e)
        {
            State = StateType.None;

            eventAggregator.GetEvent<SignalEndedEvent>().Publish(this);
        }

        private void OnSignalPaused(object sender, EventArgs e)
        {
            trackService?.Stop(Track);

            if (IsOverNextThreshold)
            {
                End();
            }
            else
            {
                eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);

                if (settingsService.Contents.Options.SelectNextOnPauseSeconds.HasValue)
                {
                    var selectNextDelay = TimeSpan.FromSeconds(
                        value: settingsService.Contents.Options.SelectNextOnPauseSeconds.Value);

                    Task.Delay(
                        delay: selectNextDelay,
                        cancellationToken: cancellationTokenSource.Token)
                        .ContinueWith(_ => SelectNext());
                }
            }
        }

        private void OnSignalUpdated(object sender, EventArgs e)
        {
            if (Position.HasValue)
            {
                if (Position.Value.Ticks - lastPosition > updatesInTicks)
                {
                    eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);

                    if (!IsOverNextThreshold
                        && nextThresholdOnPlay.HasValue)
                    {
                        IsOverNextThreshold = (Position?.Ticks > nextThresholdOnPlay.Value.Ticks)
                            || (Remaining?.Ticks < nextThresholdOnPlay.Value.Ticks);

                        if (IsOverNextThreshold)
                        {
                            SelectNext();
                        }
                    }
                }

                lastPosition = Position.Value.Ticks;
            }
        }

        private void SelectNext()
        {
            eventAggregator.GetEvent<SignalNextEvent>().Publish(this);
        }

        #endregion Private Methods
    }
}