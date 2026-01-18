using System;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core.Extensions;

namespace SportDJ.Shareds.SampleProviders.Core
{
    public class SourcePlayingProvider : BaseSampleProvider
    {
        #region Private Fields

        private readonly long? cue;
        private readonly object lockProvider = new();
        private readonly IAudioSourceProvider sourceProvider;

        private long end;

        // --- NEU: Variable Loop-Grenzen ---
        private long loopStart;
        private long loopEnd;
        // ----------------------------------

        private bool eventSentCue;
        private bool eventSentEnd;
        private bool eventSentPause;
        private bool isDisposed;
        private long position;

        #endregion Private Fields

        #region Public Constructors

        public SourcePlayingProvider(IAudioSourceProvider sourceProvider, Track track, TimeSpan fadeOut)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            Track = track;
            PlayType = PlayType.Waiting;

            cue = Track.Cue.HasValue
                ? track.Cue - fadeOut.ToBytes(averageBytesPerSecond)
                : default;

            UpdateTime();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler CueExceededEvent;
        public event EventHandler SignalEndedEvent;
        public event EventHandler SignalPausedEvent;
        public event EventHandler SignalUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan Duration { get; private set; }

        public bool IsLoop { get; set; }

        public bool IsRunning => PlayType.IsRunning();

        public PlayType PlayType { get; private set; }

        public TimeSpan Position => (position - Track.Start).ToDuration(averageBytesPerSecond);

        public TimeSpan Remaining => (end - position).ToDuration(averageBytesPerSecond);

        public Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Setzt Start und Ende des Loops.
        /// Springt SOFORT an den Start, wenn Loop aktiv ist (Beat-Match).
        /// </summary>
        public void SetLoopPoints(long startBytes, long endBytes)
        {
            lock (lockProvider)
            {
                // 1. Validierung
                if (startBytes < 0) startBytes = 0;
                if (endBytes > sourceProvider.Length) endBytes = sourceProvider.Length;

                // Sicherheitsabstand
                if (startBytes >= endBytes) startBytes = Math.Max(0, endBytes - (WaveFormat.BlockAlign * 1024));

                loopStart = startBytes;
                loopEnd = endBytes;

                // 2. Der "Instant Jump"
                // Wenn wir abspielen und loopen, zwingen wir den Player sofort an den Start.
                if (IsRunning && IsLoop)
                {
                    sourceProvider.Position = loopStart;
                    position = loopStart; // Interne Variable für UI-Update sofort nachziehen
                }
            }
        }

        /// <summary>
        /// Setzt den Loop zurück auf den gesamten Track (Start bis Cue/Ende).
        /// </summary>
        public void ResetLoopPoints()
        {
            lock (lockProvider)
            {
                loopStart = Track.Start;
                loopEnd = cue ?? end;
            }
        }

        public void End()
        {
            if (PlayType.IsActive())
            {
                if (IsLoop)
                {
                    Pause();
                }
                else
                {
                    PlayType = PlayType.Ended;
                }
            }
            else if (!eventSentEnd)
            {
                eventSentEnd = true;
                SignalEndedEvent?.Invoke(sender: this, e: default);
            }
        }

        public void Pause()
        {
            eventSentPause = false;
            PlayType = PlayType.Pausing;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = 0;
            // Hilfsgröße: Wie viele Bytes hat ein Sample (z.B. 4 bei 32-bit Float)
            int bytesPerSample = sourceProvider.WaveFormat.BitsPerSample / 8;

            try
            {
                lock (lockProvider)
                {
                    if (PlayType == PlayType.Pausing)
                    {
                        // ... (Pause-Code unverändert lassen) ...
                        if (!eventSentPause) { /* ... */ }
                        else
                        {
                            samplesRead = sourceProvider.Read(buffer, offset, count);
                            MuteBuffer(buffer, offset, count);
                            sourceProvider.Position = position;
                        }
                    }
                    else if (IsRunning)
                    {
                        if (sourceProvider.Position < Track.Start) sourceProvider.Position = Track.Start;

                        // ====================================================
                        // FIX: NAHTLOSE LOOP LOGIK
                        // ====================================================
                        if (IsLoop)
                        {
                            while (samplesRead < count)
                            {
                                // 1. Wie viel Platz ist noch im Puffer?
                                int samplesNeeded = count - samplesRead;

                                // 2. Wie viel Samples sind es noch bis zur Loop-Grenze?
                                long bytesUntilLoopEnd = loopEnd - sourceProvider.Position;
                                int samplesUntilLoopEnd = (int)(bytesUntilLoopEnd / bytesPerSample);

                                // Wenn wir schon drüber sind (oder exakt drauf), sofort springen!
                                if (samplesUntilLoopEnd <= 0)
                                {
                                    sourceProvider.Position = loopStart;
                                    // Neuberechnung nach Sprung
                                    bytesUntilLoopEnd = loopEnd - loopStart;
                                    samplesUntilLoopEnd = (int)(bytesUntilLoopEnd / bytesPerSample);
                                }

                                // 3. Wir lesen nur das Minimum: Entweder was wir brauchen, ODER was noch reinpasst
                                int toRead = Math.Min(samplesNeeded, samplesUntilLoopEnd);

                                // Sicherheit: Mindestens 0, falls Berechnung schiefging
                                if (toRead <= 0) toRead = samplesNeeded;

                                // 4. Lesen
                                int readThisTime = sourceProvider.Read(
                                    buffer,
                                    offset + samplesRead,
                                    toRead);

                                samplesRead += readThisTime;

                                // Wenn Stream zu Ende ist (EOF) oder wir nichts gelesen haben -> Break
                                if (readThisTime == 0) break;
                            }
                        }
                        // ====================================================
                        else
                        {
                            // ... (Normaler "Nicht-Loop" Code unverändert) ...
                            var lastPos = sourceProvider.Position;
                            samplesRead = sourceProvider.Read(buffer, offset, count);
                            // ... Cue/End Checks ...
                            if (sourceProvider.Position >= end || sourceProvider.Position == lastPos)
                                PlayType = PlayType.Ended;
                        }

                        position = (PlayType != PlayType.Ended) ? sourceProvider.Position : end;
                        SignalUpdatedEvent?.Invoke(sender: this, e: default);
                    }

                    if (!eventSentEnd && PlayType == PlayType.Ended)
                    {
                        eventSentEnd = true;
                        SignalEndedEvent?.Invoke(sender: this, e: default);
                    }
                }
            }
            catch { }

            return samplesRead;
        }

        public void Start()
        {
            UpdateTime();
            PlayType = PlayType.Playing;
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    End();
                    sourceProvider?.Dispose();
                }
                isDisposed = true;
            }
            base.Dispose(isDisposing);
        }

        #endregion Protected Methods

        #region Private Methods

        private void MuteBuffer(float[] buffer, int offset, int count)
        {
            var sample = 0;
            while (sample < count)
            {
                // Setze alle Kanäle auf 0 (Stille)
                // Achtung: Wenn buffer interleaved ist, ist das hier korrekt, 
                // da wir einfach 'count' floats nullen.
                buffer[offset + sample++] = 0;
            }
        }

        private void UpdateTime()
        {
            if (PlayType == PlayType.Waiting)
            {
                position = Track.Start;

                end = (Track.End ?? 0) > sourceProvider.Length
                    ? Track.End.Value
                    : sourceProvider.Length;

                // --- Initialisierung der Loop-Werte ---
                loopStart = Track.Start;
                loopEnd = cue ?? end;
                // --------------------------------------

                Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
            }
        }

        #endregion Private Methods
    }
}