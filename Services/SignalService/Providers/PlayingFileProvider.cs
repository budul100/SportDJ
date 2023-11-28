using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Models.Contents;
using SportDJ.Services.SignalService.Extensions;
using System;

namespace SportDJ.Services.SignalService.Providers
{
    internal class PlayingFileProvider
        : BaseSampleProvider, IDisposable
    {
        #region Private Fields

        private readonly long end;
        private readonly AudioFileReader reader;
        private readonly long start;

        private bool isDisposed;
        private long position;
        private StateType state;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(IEventAggregator eventAggregator, AudioFileReader reader, Track track,
            TimeSpan? begin = default, TimeSpan? length = default)
            : base(eventAggregator, reader, track)
        {
            start = begin.ToLength(averageBytesPerSecond);
            end = start + length.ToLength(averageBytesPerSecond);

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

            Duration = (end - start).ToTime(averageBytesPerSecond);
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan Current => (position - start).ToTime(averageBytesPerSecond);

        public TimeSpan Duration { get; }

        public bool IsLoop { get; set; }

        public TimeSpan Remaining => (end - position).ToTime(averageBytesPerSecond);

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

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

                            if ((reader.Position >= end) || (reader.Position == lastPosition))
                            {
                                state = StateType.None;
                            }
                        }

                        if (state == StateType.None)
                        {
                            position = end;
                            eventAggregator.GetEvent<SignalEndedEvent>().Publish(track);
                        }
                        else
                        {
                            position = reader.Position;
                            eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(track);
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

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    state = StateType.None;

                    reader?.Dispose();
                }

                isDisposed = true;
            }
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

        #endregion Private Methods
    }
}