using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Logging;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core;
using SportDJ.Shareds.SampleProviders.Windows;

namespace SportDJ.Services.SignalService.Windows;

public class Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
    ITrackService trackService, ISourceProvider sourceProvider)
    : ISignalService
{
    #region Private Fields

    // Events raised by the providers run inside the audio callback and must never block it.
    // They are queued here and processed in order by a single consumer outside the audio thread.
    private readonly Channel<Action> audioEvents = Channel.CreateUnbounded<Action>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly long updatesInTicks = Constants.UpdatesInMilliSec * 10000;

    private Task eventLoop;
    private FadingProvider fadingProvider;
    private volatile bool isDisposed;
    private volatile bool isOverNextThreshold;
    private long lastPosition = 0;
    private LoopingProvider loopingProvider;
    private TimeSpan? nextThresholdOnPlay;
    private PlayingProvider playingProvider;
    private TimeSpan? selectNextOnPlayDuration;
    private volatile StateType state;

    #endregion Private Fields

    #region Public Properties

    public TimeSpan? Current => playingProvider?.Current;

    public TimeSpan? Duration => playingProvider?.Duration;

    public bool IsEnding => playingProvider?.IsRunning == true
        && playingProvider.Remaining <= TimeSpan.FromSeconds(Constants.IsEndingSeconds);

    public bool IsLoop => loopingProvider?.IsLoop == true;

    public bool IsLoopReady => loopingProvider?.IsReady == true;

    public bool IsOverNextThreshold => isOverNextThreshold;

    public bool IsPlaying => playingProvider?.PlayType == PlayType.Playing;

    public bool IsSilent => fadingProvider?.IsSilent != false;

    public TimeSpan? LoopEnd => loopingProvider?.End;

    public TimeSpan? LoopStart => loopingProvider?.Start;

    public ISampleProvider Output => fadingProvider;

    public double? Progress => Duration?.Ticks > 0
        ? (Current?.Ticks ?? 0) / (double)Duration.Value.Ticks
        : default;

    public TimeSpan? Remaining => playingProvider?.Remaining;

    public StateType State
    {
        get => state;
        private set => state = value;
    }

    public Track Track { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void ActivateLoop()
    {
        loopingProvider?.Activate();
    }

    public void DeactivateLoop()
    {
        loopingProvider?.Deactivate();
    }

    public void Dispose()
    {
        Dispose(isDisposing: true);
        GC.SuppressFinalize(obj: this);
    }

    public void End()
    {
        State = StateType.None;

        playingProvider?.End();
    }

    public void Initialize(Track track, WaveFormat waveFormat)
    {
        ArgumentNullException.ThrowIfNull(track);

        if (File.Exists(track.Path) && State == StateType.None)
        {
            Track = track;

            eventLoop ??= Task.Run(ProcessAudioEvents);

            var fadeOut = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0
                ? TimeSpan.FromMilliseconds(settingsService.Contents.Options.FadeOnMixOutMilliseconds)
                : TimeSpan.Zero;

            sourceProvider.Initialize(Track.Path);

            loopingProvider = new LoopingProvider(
                sourceProvider: sourceProvider,
                track: Track);

            loopingProvider.LoopReadyEvent += OnLoopReady;
            loopingProvider.LoopActivateEvent += OnLoopActivated;
            loopingProvider.LoopDeactivateEvent += OnLoopDeactivated;

            playingProvider = new PlayingProvider(
                sourceProvider: loopingProvider,
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

            fadingProvider = new FadingProvider(resampleProvider);

            fadingProvider.FadingStartedEvent += OnFadingStarted;
            fadingProvider.FadingEndedEvent += OnFadingEnded;

            if (settingsService.Contents.Options.SelectNextOnPlaySeconds.HasValue)
            {
                var selectNextSeconds = settingsService.Contents.Options
                    .SelectNextOnPlaySeconds.Value;
                var selectNextDuration = TimeSpan.FromSeconds(selectNextSeconds);

                selectNextOnPlayDuration = selectNextDuration;
                nextThresholdOnPlay = playingProvider.Current.Add(selectNextDuration);
            }

            eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);
        }
    }

    public void Pause(bool useFilterEffect = false)
    {
        if (playingProvider != default
            && (State == StateType.Play || State == StateType.Stop))
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
                audioEvents.Writer.TryComplete();
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();

                if (fadingProvider != default)
                {
                    fadingProvider.FadingStartedEvent -= OnFadingStarted;
                    fadingProvider.FadingEndedEvent -= OnFadingEnded;

                    fadingProvider.Dispose();
                }

                if (playingProvider != default)
                {
                    playingProvider.CueExceededEvent -= OnCueExceeded;
                    playingProvider.SignalPausedEvent -= OnSignalPaused;
                    playingProvider.SignalEndedEvent -= OnSignalEnded;
                    playingProvider.SignalUpdatedEvent -= OnSignalUpdated;

                    playingProvider.Dispose();
                }

                if (loopingProvider != default)
                {
                    loopingProvider.LoopReadyEvent -= OnLoopReady;
                    loopingProvider.LoopActivateEvent -= OnLoopActivated;
                    loopingProvider.LoopDeactivateEvent -= OnLoopDeactivated;

                    loopingProvider.Dispose();
                }

                Track = default;
            }
            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void Enqueue(Action action)
    {
        audioEvents.Writer.TryWrite(action);
    }

    private void HandleCueExceeded()
    {
        if (isDisposed)
        {
            return;
        }

        trackService?.Stop(Track);

        State = StateType.Stop;

        Pause();
    }

    private void HandleSignalEnded()
    {
        if (isDisposed)
        {
            return;
        }

        State = StateType.None;

        eventAggregator.GetEvent<SignalEndedEvent>().Publish(this);
    }

    private void HandleSignalPaused()
    {
        if (isDisposed)
        {
            return;
        }

        trackService?.Stop(Track);

        eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);

        if (IsOverNextThreshold)
        {
            End();
        }
        else
        {
            if (settingsService.Contents.Options.SelectNextOnPauseSeconds.HasValue)
            {
                var selectNextDelay = TimeSpan.FromSeconds(
                    value: settingsService.Contents.Options.SelectNextOnPauseSeconds.Value);

                Task.Delay(
                    delay: selectNextDelay,
                    cancellationToken: cancellationTokenSource.Token)
                    .ContinueWith(
                        continuationAction: _ => SelectNext(),
                        continuationOptions: TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }
    }

    private void OnCueExceeded(object sender, EventArgs e)
    {
        Enqueue(HandleCueExceeded);
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
            if (State != StateType.Stop)
            {
                State = StateType.Pause;
            }
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

    private void OnLoopActivated(object sender, EventArgs e)
    {
        eventAggregator.GetEvent<LoopActivatedEvent>().Publish(this);
    }

    private void OnLoopDeactivated(object sender, EventArgs e)
    {
        eventAggregator.GetEvent<LoopDeactivatedEvent>().Publish(this);
    }

    private void OnLoopReady(object sender, EventArgs e)
    {
        eventAggregator.GetEvent<LoopReadyEvent>().Publish(this);
    }

    private void OnSignalEnded(object sender, EventArgs e)
    {
        Enqueue(HandleSignalEnded);
    }

    private void OnSignalPaused(object sender, EventArgs e)
    {
        Enqueue(HandleSignalPaused);
    }

    private void OnSignalUpdated(object sender, EventArgs e)
    {
        if (Current.HasValue)
        {
            if (Current.Value.Ticks - lastPosition > updatesInTicks)
            {
                eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);

                if (!IsOverNextThreshold
                    && nextThresholdOnPlay.HasValue)
                {
                    isOverNextThreshold = Current?.Ticks > nextThresholdOnPlay.Value.Ticks
                        || Remaining?.Ticks < selectNextOnPlayDuration.Value.Ticks;

                    if (isOverNextThreshold)
                    {
                        // Selecting the next track loads and decodes a file, never do that in the audio thread
                        Enqueue(SelectNext);
                    }
                }
            }

            lastPosition = Current.Value.Ticks;
        }
    }

    private async Task ProcessAudioEvents()
    {
        try
        {
            await foreach (var action in audioEvents.Reader.ReadAllAsync(cancellationTokenSource.Token))
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    // A failing handler must not stop the processing of later events
                    Log.Error("Handling an audio event failed.", exception);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed
        }
    }

    private void SelectNext()
    {
        eventAggregator.GetEvent<SignalNextEvent>().Publish(this);
    }

    #endregion Private Methods
}
