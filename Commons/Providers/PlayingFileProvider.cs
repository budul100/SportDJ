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

        private readonly long end;
        private readonly AudioFileReader reader;
        private readonly long start;

        private long position;
        private StateType state;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(AudioFileReader reader, Track track, TimeSpan? begin = default,
            TimeSpan? length = default)
            : base(sourceProvider: reader)
        {
            Track = track;

            start = begin.ToSeconds(averageBytesPerSecond);
            end = start + length.ToSeconds(averageBytesPerSecond);

            if (start > end)
            {
                throw new ArgumentException(
                    message: "Playing start cannot be greater than playing end.",
                    paramName: nameof(begin));
            }

            if (end > reader.Length)
            {
                end = reader.Length;
            }

            Duration = (end - start).ToDuration(averageBytesPerSecond);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler SignalEndedEvent;

        public event EventHandler SignalUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan Current => (position - start).ToDuration(averageBytesPerSecond);

        public TimeSpan Duration { get; }

        public bool IsLoop { get; set; }

        public TimeSpan Remaining => (end - position).ToDuration(averageBytesPerSecond);

        public Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        public void Pause()
        {
            state = StateType.Pause;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var read = 0;

            try
            {
                lock (reader)
                {
                    if (state == StateType.Pause)
                    {
                        read = reader.Read(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        MuteBuffer(
                            buffer: buffer,
                            offset: offset,
                            count: count);

                        reader.Position = position;
                    }
                    else if (state == StateType.Play)
                    {
                        if (reader.Position < start)
                        {
                            reader.Position = start;
                        }

                        if (IsLoop)
                        {
                            while (read < count)
                            {
                                var required = count - read;
                                var readThisTime = reader.Read(buffer, offset + read, required);

                                if (readThisTime < required || reader.Position >= end)
                                {
                                    reader.Position = start;
                                }
                                read += readThisTime;
                            }
                        }
                        else
                        {
                            var lastPosition = reader.Position;

                            read = reader.Read(buffer, offset, count);

                            if (reader.Position >= end || reader.Position == lastPosition)
                            {
                                state = StateType.None;
                            }
                        }

                        if (state == StateType.None)
                        {
                            position = end;

                            OnSignalEnded();
                        }
                        else
                        {
                            position = reader.Position;

                            OnSignalUpdated();
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
            state = StateType.Play;
        }

        public void Stop()
        {
            state = StateType.None;
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            state = StateType.None;

            reader?.Dispose();

            base.Dispose(isDisposing);
        }

        #endregion Protected Methods

        #region Private Methods

        private void MuteBuffer(float[] buffer, int offset, int count)
        {
            var sample = 0;
            while (sample < count)
            {
                for (var ch = 0; ch < reader.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] = 0;
                }
            }
        }

        private void OnSignalEnded()
        {
            SignalEndedEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void OnSignalUpdated()
        {
            SignalUpdatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}