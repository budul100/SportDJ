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

        private readonly long? cue;
        private readonly object lockProvider = new();
        private readonly IPlayingProvider sourceProvider;

        private long end;
        private bool eventSentCue;
        private bool eventSentEnd;
        private bool eventSentPause;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public PlayingProvider(IPlayingProvider sourceProvider, Track track, TimeSpan fadeOut)
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

        public TimeSpan Current => (Position - Track.Start).ToDuration(averageBytesPerSecond);

        public TimeSpan Duration { get; private set; }

        public bool IsRunning => PlayType.IsRunning();

        public PlayType PlayType { get; private set; }

        public long Position { get; private set; }

        public TimeSpan Remaining => (end - Position).ToDuration(averageBytesPerSecond);

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
                eventSentPause = false;

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
                    if (!eventSentPause)
                    {
                        SignalUpdatedEvent?.Invoke(
                            sender: this,
                            e: default);

                        eventSentPause = true;

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
                        if (sourceProvider.Position < Track.Start)
                        {
                            sourceProvider.Position = Track.Start;
                        }

                        samplesRead = sourceProvider.Read(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        position = sourceProvider.Position;
                    }

                    if (!eventSentCue
                        && cue.HasValue
                            && position >= cue)
                    {
                        PlayType = PlayType.Stopping;

                        eventSentCue = true;

                        CueExceededEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    if (position >= end)
                    {
                        PlayType = PlayType.Ended;
                    }

                    Position = PlayType != PlayType.Ended
                        ? position
                        : end;

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
            if (!eventSentEnd)
            {
                eventSentEnd = true;

                SignalEndedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        private void UpdateTime()
        {
            if (PlayType == PlayType.Waiting)
            {
                Position = Track.Start;

                end = !Track.End.HasValue || Track.End.Value > sourceProvider.Length
                    ? sourceProvider.Length
                    : Track.End.Value;

                Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
            }
        }

        #endregion Private Methods
    }
}