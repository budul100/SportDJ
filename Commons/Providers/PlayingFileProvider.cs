using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Providers.Extensions;
using System;

namespace SportDJ.Commons.Providers
{
    public class PlayingFileProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly long? cue;
        private readonly object lockProvider = new();
        private readonly AudioFileReader sourceProvider;

        private long end;
        private bool eventSentCue;
        private bool eventSentEnd;
        private bool eventSentPause;
        private bool isDisposed;
        private long position;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(AudioFileReader sourceProvider, Track track, TimeSpan fadeOut)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            PlayType = PlayType.Waiting;
            Track = track;

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

                SignalEndedEvent?.Invoke(
                    sender: this,
                    e: default);
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

            try
            {
                lock (lockProvider)
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
                        else
                        {
                            samplesRead = sourceProvider.Read(
                                buffer: buffer,
                                offset: offset,
                                count: count);

                            MuteBuffer(
                                buffer: buffer,
                                offset: offset,
                                count: count);

                            sourceProvider.Position = position;
                        }
                    }
                    else if (IsRunning)
                    {
                        if (sourceProvider.Position < Track.Start)
                        {
                            sourceProvider.Position = Track.Start;
                        }

                        if (IsLoop)
                        {
                            while (samplesRead < count)
                            {
                                var required = count - samplesRead;

                                var readThisTime = sourceProvider.Read(
                                    buffer: buffer,
                                    offset: offset + samplesRead,
                                    count: required);

                                if (readThisTime < required || sourceProvider.Position >= end
                                    || (cue.HasValue && sourceProvider.Position >= cue))
                                {
                                    sourceProvider.Position = Track.Start;
                                }

                                samplesRead += readThisTime;
                            }
                        }
                        else
                        {
                            var lastPosition = sourceProvider.Position;

                            samplesRead = sourceProvider.Read(
                                buffer: buffer,
                                offset: offset,
                                count: count);

                            if (!eventSentCue
                                && cue.HasValue
                                && sourceProvider.Position >= cue)
                            {
                                PlayType = PlayType.Stopping;

                                eventSentCue = true;

                                CueExceededEvent?.Invoke(
                                    sender: this,
                                    e: default);
                            }

                            if (sourceProvider.Position >= end || sourceProvider.Position == lastPosition)
                            {
                                PlayType = PlayType.Ended;
                            }
                        }

                        position = PlayType != PlayType.Ended
                            ? sourceProvider.Position
                            : end;

                        SignalUpdatedEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    if (!eventSentEnd
                        && PlayType == PlayType.Ended)
                    {
                        eventSentEnd = true;

                        SignalEndedEvent?.Invoke(
                            sender: this,
                            e: default);
                    }
                }
            }
            catch
            { }

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
                for (var ch = 0; ch < sourceProvider.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] = 0;
                }
            }
        }

        private void UpdateTime()
        {
            if (PlayType == PlayType.Waiting)
            {
                position = Track.Start;

                end = (Track.End ?? 0) > this.sourceProvider.Length
                    ? Track.End.Value
                    : this.sourceProvider.Length;

                Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
            }
        }

        #endregion Private Methods
    }
}