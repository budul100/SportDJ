using LoopDetector;
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
using System;
using System.Collections.Generic; // NEU
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        // --- NEU: Analyse Felder ---
        private float[] _analysisData;
        private bool _isAnalysisReady;
        private int _bytesPerFrame; // Wichtig für Umrechnung
                                    // ---------------------------

        #endregion Private Fields

        // ... [Deine bestehenden Properties bleiben unverändert] ...
        #region Public Properties

        public TimeSpan? Duration => !(playingProvider?.Duration < TimeSpan.Zero)
            ? playingProvider?.Duration
            : TimeSpan.Zero;

        public bool IsEnding => playingProvider?.IsRunning == true
            && playingProvider.Remaining <= TimeSpan.FromSeconds(Constants.IsEndingSeconds);

        // Anpassung: Setter muss jetzt ggf. die Loop-Punkte im Provider aktivieren
        public bool IsLoop
        {
            get => playingProvider?.IsLoop ?? default;
            set
            {
                if (playingProvider != default)
                {
                    ActivateSmartLoop();

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

                // Reset Analyse Status
                _isAnalysisReady = false;
                _analysisData = null;

                var fadeOut = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0
                    ? TimeSpan.FromMilliseconds(settingsService.Contents.Options.FadeOnMixOutMilliseconds)
                    : TimeSpan.Zero;

                // 1. Playback initialisieren (Das muss schnell gehen -> UI Thread)
                sourceProvider.Initialize(Track.Path);

                playingProvider = new SourcePlayingProvider(
                    sourceProvider: sourceProvider,
                    track: Track,
                    fadeOut: fadeOut);

                // Format für Byte-Umrechnung merken
                _bytesPerFrame = playingProvider.WaveFormat.BlockAlign;

                // 2. NEU: Analyse im Hintergrund starten ("Fire and Forget")
                // Wir übergeben den Pfad und das Format.
                // WICHTIG: Nutze FileShare.Read, damit sourceProvider und Analyse gleichzeitig lesen können.
                Task.Run(() => PerformBackgroundAnalysis(Track.Path, playingProvider.WaveFormat));

                // ... [Rest deiner Initialisierung bleibt gleich] ...
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

        public void ActivateSmartLoop()
        {
            // 1. Checks
            if (!_isAnalysisReady || _analysisData == null || playingProvider == null) return;

            // 2. Aktuelle Position holen
            double currentSeconds = playingProvider.Position.TotalSeconds;
            long currentSamplePos = (long)(currentSeconds * playingProvider.WaveFormat.SampleRate);

            // 3. Loop suchen (Rückwärts + Quantisiert)
            var result = LoopDetector.SampleLoopDetector.FindQuantizedLoopBackwards(_analysisData, currentSamplePos, minLoopSeconds: 2.0);

            if (result.IsValid)
            {
                // 4. Umrechnen in Bytes
                long startByte = result.StartSample * _bytesPerFrame;
                long endByte = result.EndSample * _bytesPerFrame;

                // BlockAlign runden (Wichtig!)
                startByte -= startByte % _bytesPerFrame;
                endByte -= endByte % _bytesPerFrame;

                // 5. Anwenden
                if (playingProvider is SourcePlayingProvider loopable)
                {
                    loopable.IsLoop = true;

                    // Dieser Aufruf setzt die Grenzen UND springt intern sofort an die richtige Stelle.
                    // Du musst hier nichts mehr manuell an der Position drehen.
                    loopable.SetLoopPoints(startByte, endByte);
                }
            }
        }



        public void Pause(bool useFilterEffect = false)
        {
            // ... [Unverändert] ...
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
            // ... [Unverändert] ...
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

                    // NEU: Daten aufräumen
                    _analysisData = null;

                    if (playingProvider != default)
                    {
                        // ... [Unverändert] ...
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

        // --- NEU: Hintergrund Analyse ---
        private void PerformBackgroundAnalysis(string path, WaveFormat format)
        {
            try
            {
                // Datei schreibgeschützt öffnen, damit Playback nicht blockiert wird
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamMediaFoundationReader(fs))
                {
                    // Hier die Mono-Konvertierung (wie zuvor besprochen)
                    var buffer = new List<float>();
                    var sampleProvider = reader.ToSampleProvider();
                    var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                    int read;

                    // Abbruchbedingung prüfen (falls User schon Track gewechselt hat)
                    while ((read = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                    {
                        if (isDisposed) return; // Abbruch

                        for (int i = 0; i < read; i += reader.WaveFormat.Channels)
                        {
                            float sum = 0;
                            for (int c = 0; c < reader.WaveFormat.Channels; c++) sum += readBuffer[i + c];
                            buffer.Add(sum / reader.WaveFormat.Channels);
                        }
                    }
                    _analysisData = buffer.ToArray();
                    _isAnalysisReady = true;
                }
            }
            catch (Exception ex)
            {
                // Fehler loggen, aber nicht abstürzen (Playback läuft ja)
                System.Diagnostics.Debug.WriteLine($"Analyse fehlgeschlagen: {ex.Message}");
            }
        }

        // ... [Deine restlichen Private Methods: OnCueExceeded, OnFadingEnded etc. bleiben unverändert] ...
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
                trackService.Play(track: Track, isPause: isPause);
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
                    var selectNextDelay = TimeSpan.FromSeconds(value: settingsService.Contents.Options.SelectNextOnPauseSeconds.Value);
                    Task.Delay(delay: selectNextDelay, cancellationToken: cancellationTokenSource.Token)
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
                    if (!IsOverNextThreshold && nextThresholdOnPlay.HasValue)
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