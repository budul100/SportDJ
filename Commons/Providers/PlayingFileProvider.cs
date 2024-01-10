using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Providers
{
    public class PlayingFileProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly AudioFileReader sourceProvider;
        private long end;
        private bool isCue;
        private bool isPause;
        private long position;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(AudioFileReader sourceProvider, Track track)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            Track = track;

            UpdateTime();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler SignalCompletedEvent;

        public event EventHandler SignalStoppedEvent;

        public event EventHandler SignalUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan Duration { get; private set; }

        public bool IsLoop { get; set; }

        public TimeSpan Position => (position - Track.Start).ToDuration(averageBytesPerSecond);

        public TimeSpan Remaining => (end - position).ToDuration(averageBytesPerSecond);

        public StateType State { get; private set; }

        public Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        public void Pause()
        {
            State = StateType.Pause;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var read = 0;

            try
            {
                lock (sourceProvider)
                {
                    if (State == StateType.Pause)
                    {
                        if (!isPause)
                        {
                            SignalStoppedEvent?.Invoke(
                                sender: this,
                                e: default);

                            isPause = true;
                        }
                        else
                        {
                            read = sourceProvider.Read(
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
                    else
                    {
                        isPause = false;

                        if (State.IsPlaying())
                        {
                            if (sourceProvider.Position < Track.Start)
                            {
                                sourceProvider.Position = Track.Start;
                            }

                            if (IsLoop)
                            {
                                while (read < count)
                                {
                                    var required = count - read;

                                    var readThisTime = sourceProvider.Read(
                                        buffer: buffer,
                                        offset: offset + read,
                                        count: required);

                                    if (readThisTime < required || sourceProvider.Position >= end)
                                    {
                                        sourceProvider.Position = Track.Start;
                                    }

                                    read += readThisTime;
                                }
                            }
                            else
                            {
                                var lastPosition = sourceProvider.Position;

                                read = sourceProvider.Read(
                                    buffer: buffer,
                                    offset: offset,
                                    count: count);

                                if (Track.Cue.HasValue
                                    && sourceProvider.Position >= Track.Cue
                                    && !isCue)
                                {
                                    State = StateType.Cue;

                                    SignalStoppedEvent?.Invoke(
                                        sender: this,
                                        e: default);

                                    isCue = true;
                                }
                                else if (sourceProvider.Position >= end || sourceProvider.Position == lastPosition)
                                {
                                    State = StateType.None;
                                }
                            }

                            if (State == StateType.None)
                            {
                                position = end;

                                if (!isCue)
                                {
                                    SignalStoppedEvent?.Invoke(
                                        sender: this,
                                        e: default);

                                    isCue = true;
                                }

                                SignalCompletedEvent?.Invoke(
                                    sender: this,
                                    e: default);
                            }
                            else
                            {
                                position = sourceProvider.Position;

                                SignalUpdatedEvent?.Invoke(
                                    sender: this,
                                    e: default);
                            }
                        }
                    }
                }
            }
            catch
            { }

            return read;
        }

        public void Start()
        {
            UpdateTime();

            State = StateType.Play;
        }

        public void Stop()
        {
            State = StateType.None;
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            State = StateType.None;

            sourceProvider?.Dispose();

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
            end = (Track.End ?? 0) > this.sourceProvider.Length
                ? Track.End.Value
                : this.sourceProvider.Length;

            Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
        }

        #endregion Private Methods
    }
}