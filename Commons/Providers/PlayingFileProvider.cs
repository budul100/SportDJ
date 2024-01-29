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
        private long position;
        private bool sentPause;
        private bool sentStop;

        #endregion Private Fields

        #region Public Constructors

        public PlayingFileProvider(AudioFileReader sourceProvider, Track track)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            PlayType = PlayType.Wait;
            Track = track;

            UpdateTime();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler SignalEndedEvent;

        public event EventHandler SignalPausedEvent;

        public event EventHandler SignalStoppedEvent;

        public event EventHandler SignalUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan Duration { get; private set; }

        public bool IsLoop { get; set; }

        public PlayType PlayType { get; private set; }

        public TimeSpan Position => (position - Track.Start).ToDuration(averageBytesPerSecond);

        public TimeSpan Remaining => (end - position).ToDuration(averageBytesPerSecond);

        public Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        public void End()
        {
            PlayType = PlayType.End;
        }

        public void Pause()
        {
            sentPause = false;

            PlayType = PlayType.Pause;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var read = 0;

            try
            {
                lock (sourceProvider)
                {
                    if (PlayType == PlayType.Pause)
                    {
                        if (!sentPause)
                        {
                            SignalUpdatedEvent?.Invoke(
                                sender: this,
                                e: default);

                            SignalPausedEvent?.Invoke(
                                sender: this,
                                e: default);

                            sentPause = true;
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
                    else if (PlayType == PlayType.Play || PlayType == PlayType.Stop)
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

                            if (!sentStop
                                && Track.Cue.HasValue
                                && sourceProvider.Position >= Track.Cue)
                            {
                                PlayType = PlayType.Stop;

                                SignalStoppedEvent?.Invoke(
                                    sender: this,
                                    e: default);

                                sentStop = true;
                            }

                            if (sourceProvider.Position >= end || sourceProvider.Position == lastPosition)
                            {
                                PlayType = PlayType.End;
                            }
                        }

                        position = PlayType != PlayType.End
                            ? sourceProvider.Position
                            : end;

                        SignalUpdatedEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    if (PlayType == PlayType.End)
                    {
                        SignalEndedEvent?.Invoke(
                            sender: this,
                            e: default);
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

            PlayType = PlayType.Play;
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            End();

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
            if (PlayType == PlayType.Wait)
            {
                end = (Track.End ?? 0) > this.sourceProvider.Length
                    ? Track.End.Value
                    : this.sourceProvider.Length;

                Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
            }
        }

        #endregion Private Methods
    }
}