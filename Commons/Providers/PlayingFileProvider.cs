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
        private readonly AudioFileReader sourceProvider;
        private readonly long start;

        private long position;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(AudioFileReader sourceProvider, Track track, TimeSpan? begin = default,
            TimeSpan? length = default)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            Track = track;
            start = begin.ToSeconds(averageBytesPerSecond);
            end = start + length.ToSeconds(averageBytesPerSecond);

            if (start > end)
            {
                throw new ArgumentException(
                    message: "Playing start cannot be greater than playing end.",
                    paramName: nameof(begin));
            }

            if (end > this.sourceProvider.Length)
            {
                end = this.sourceProvider.Length;
            }

            Duration = (end - start).ToDuration(averageBytesPerSecond);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler SignalEndedEvent;

        public event EventHandler SignalUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan Position => (position - start).ToDuration(averageBytesPerSecond);

        public TimeSpan Duration { get; }

        public bool IsLoop { get; set; }

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
                    else if (State == StateType.Play)
                    {
                        if (sourceProvider.Position < start)
                        {
                            sourceProvider.Position = start;
                        }

                        if (IsLoop)
                        {
                            while (read < count)
                            {
                                var required = count - read;
                                var readThisTime = sourceProvider.Read(buffer, offset + read, required);

                                if (readThisTime < required || sourceProvider.Position >= end)
                                {
                                    sourceProvider.Position = start;
                                }
                                read += readThisTime;
                            }
                        }
                        else
                        {
                            var lastPosition = sourceProvider.Position;

                            read = sourceProvider.Read(buffer, offset, count);

                            if (sourceProvider.Position >= end || sourceProvider.Position == lastPosition)
                            {
                                State = StateType.None;
                            }
                        }

                        if (State == StateType.None)
                        {
                            position = end;

                            OnSignalEnded();
                        }
                        else
                        {
                            position = sourceProvider.Position;

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