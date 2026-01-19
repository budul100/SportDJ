using System;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core.Extensions;

namespace SportDJ.Shareds.SampleProviders.Core
{
    public class PlayingProvider
        : BaseProvider
    {
        #region Private Fields

        private readonly object lockProvider = new();
        private readonly long sampleFade;
        private readonly IPlayingProvider sourceProvider;

        private bool isDisposed;
        private bool publishedCue;
        private bool publishedEnd;
        private bool publishedPause;
        private long sampleEnd;
        private long sampleStart;
        private long sampleStop;

        #endregion Private Fields

        #region Public Constructors

        public PlayingProvider(IPlayingProvider sourceProvider, Track track, TimeSpan fadeOut)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            Track = track;
            PlayType = PlayType.Waiting;

            sampleFade = fadeOut.ToSamples(WaveFormat.SampleRate);

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

        public TimeSpan Current => (Position - sampleStart).ToDuration(WaveFormat.SampleRate);

        public TimeSpan Duration { get; private set; }

        public bool IsRunning => PlayType.IsRunning();

        public PlayType PlayType { get; private set; }

        public long Position { get; private set; }

        public TimeSpan Remaining => (sampleEnd - Position).ToDuration(WaveFormat.SampleRate);

        public Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        public void End()
        {
            if (!PlayType.IsActive())
            {
                SendSignalEnded();
            }
            else
            {
                lock (lockProvider)
                {
                    PlayType = PlayType.Ended;
                }
            }
        }

        public void Pause()
        {
            lock (lockProvider)
            {
                publishedPause = false;

                PlayType = PlayType.Pausing;
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = 0;

            try
            {
                if (PlayType == PlayType.Pausing)
                {
                    if (!publishedPause)
                    {
                        SignalUpdatedEvent?.Invoke(
                            sender: this,
                            e: default);

                        publishedPause = true;

                        SignalPausedEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    lock (lockProvider)
                    {
                        samplesRead = sourceProvider.Read(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        MuteBuffer(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        sourceProvider.Position = Position;
                    }
                }
                else if (IsRunning)
                {
                    var position = default(long);

                    lock (lockProvider)
                    {
                        if (sourceProvider.Position < sampleStart)
                        {
                            sourceProvider.Position = sampleStart;
                        }

                        samplesRead = sourceProvider.Read(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        position = sourceProvider.Position;
                    }

                    if (!publishedCue
                        && position >= sampleStop)
                    {
                        PlayType = PlayType.Stopping;

                        publishedCue = true;

                        CueExceededEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    if (position >= sampleEnd
                        || (samplesRead == 0 && position > sampleStart))
                    {
                        PlayType = PlayType.Ended;
                    }

                    Position = PlayType != PlayType.Ended
                        ? position
                        : sampleEnd;

                    SignalUpdatedEvent?.Invoke(
                        sender: this,
                        e: default);
                }

                if (PlayType == PlayType.Ended)
                {
                    SendSignalEnded();
                }
            }
            catch { }

            return samplesRead;
        }

        public void Start()
        {
            lock (lockProvider)
            {
                UpdateTime();

                PlayType = PlayType.Playing;
            }
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

        private static void MuteBuffer(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = 0;
            }
        }

        private void SendSignalEnded()
        {
            if (!publishedEnd)
            {
                publishedEnd = true;

                SignalEndedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        private void UpdateTime()
        {
            if (PlayType == PlayType.Waiting)
            {
                sampleStart = Track.Start.ToSamples(WaveFormat.BlockAlign);

                var trackEnd = Track.End.HasValue
                    ? Track.End.Value.ToSamples(WaveFormat.BlockAlign)
                    : default(long?);

                sampleEnd = !trackEnd.HasValue || trackEnd.Value > sourceProvider.Length
                    ? sourceProvider.Length
                    : trackEnd.Value;

                var trackCue = Track.Cue.HasValue
                    ? Track.Cue.Value.ToSamples(WaveFormat.BlockAlign)
                    : default(long?);

                sampleStop = (trackCue ?? sampleEnd) - sampleFade;

                Duration = (sampleEnd - sampleStart).ToDuration(WaveFormat.SampleRate);

                Position = sampleStart;
            }
        }

        #endregion Private Methods
    }
}