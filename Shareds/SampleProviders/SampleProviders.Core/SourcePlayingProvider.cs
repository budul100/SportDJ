using System;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core.Extensions;

namespace SportDJ.Shareds.SampleProviders.Core
{
    public class SourcePlayingProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly long? cue;
        private readonly object lockProvider = new();
        private readonly IAudioSourceProvider sourceProvider;

        private long end;
        private bool eventSentCue;
        private bool eventSentEnd;
        private bool eventSentPause;
        private bool isDisposed;
        private long loopEnd;
        private long loopStart;
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
            var bytesPerSample = sourceProvider.WaveFormat.BitsPerSample / 8;

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
                                var samplesNeeded = count - samplesRead;

                                var bytesUntilLoopEnd = loopEnd - sourceProvider.Position;
                                var samplesUntilLoopEnd = (int)(bytesUntilLoopEnd / bytesPerSample);

                                if (samplesUntilLoopEnd <= 0)
                                {
                                    sourceProvider.Position = loopStart;
                                    bytesUntilLoopEnd = loopEnd - loopStart;

                                    samplesUntilLoopEnd = (int)(bytesUntilLoopEnd / bytesPerSample);
                                }

                                var toRead = Math.Min(samplesNeeded, samplesUntilLoopEnd);
                                if (toRead <= 0) toRead = samplesNeeded;

                                var samplesLoop = sourceProvider.Read(
                                    buffer: buffer,
                                    offset: offset + samplesRead,
                                    count: toRead);

                                samplesRead += samplesLoop;
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

        public void ResetLoop()
        {
            lock (lockProvider)
            {
                loopStart = Track.Start;
                loopEnd = cue ?? end;
            }
        }

        public void SetLoop(long startBytes, long endBytes)
        {
            lock (lockProvider)
            {
                if (startBytes < 0) startBytes = 0;
                if (endBytes > sourceProvider.Length) endBytes = sourceProvider.Length;

                if (startBytes >= endBytes) startBytes = Math.Max(
                    val1: 0,
                    val2: endBytes - (WaveFormat.BlockAlign * 1024));

                loopStart = startBytes;
                loopEnd = endBytes;
            }
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

        private static void MuteBuffer(float[] buffer, int offset, int count)
        {
            var sample = 0;

            while (sample < count)
            {
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

                Duration = (end - Track.Start).ToDuration(averageBytesPerSecond);
            }
        }

        #endregion Private Methods
    }
}