using NAudio.Wave;
using Score2Stream.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Extensions;
using System;

namespace SportDJ.Commons.Providers
{
    public class FadingSampleProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private long fadeSampleCount;
        private long fadeSamplePosition;
        private float volumeCurrent;
        private float volumeFadeDiff;
        private float volumeFadeStart;

        #endregion Private Fields

        #region Public Constructors

        public FadingSampleProvider(ISampleProvider sourceProvider, float volume = 0f)
            : base(sourceProvider: sourceProvider)
        {
            Volume = volume;
            volumeCurrent = Volume;
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler SignalFadedEvent;

        public event EventHandler SignalStartedEvent;

        #endregion Public Events

        #region Public Properties

        public FadeType FadeType { get; private set; }

        public bool IsSilent => Volume == Constants.VolumeMin;

        public float Volume { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Fade(float volume, int milliSeconds)
        {
            var duration = milliSeconds.ToDuration();

            Fade(
                volume: volume,
                duration: duration);
        }

        public void Fade(float volume, TimeSpan? duration = default)
        {
            if (volume < Constants.VolumeMin || volume > Constants.VolumeMax)
            {
                throw new ArgumentOutOfRangeException(nameof(volume));
            }

            if (volume != Volume)
            {
                fadeSampleCount = duration.ToSeconds(averageBytesPerSecond);
                Volume = volume;

                StartFade(Volume);
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var read = sourceProvider.Read(
                buffer: buffer,
                offset: offset,
                count: count);

            lock (sourceProvider)
            {
                AdjustBuffer(
                    buffer: buffer,
                    offset: offset,
                    count: read);
            }

            return read;
        }

        #endregion Public Methods

        #region Private Methods

        private void AdjustBuffer(float[] buffer, int offset, int count)
        {
            var sample = 0;

            while (sample < count)
            {
                if (FadeType == FadeType.In)
                {
                    volumeCurrent = volumeFadeStart + (fadeSamplePosition * volumeFadeDiff);
                }
                else if (FadeType == FadeType.Out)
                {
                    volumeCurrent = volumeFadeStart - (fadeSamplePosition * volumeFadeDiff);
                }

                for (var ch = 0; ch < sourceProvider.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= volumeCurrent;
                }

                fadeSamplePosition++;

                if (FadeType != FadeType.Complete
                    && (volumeCurrent == Volume || fadeSamplePosition > fadeSampleCount))
                {
                    OnSignalFaded();
                }
            }
        }

        private void OnSignalFaded()
        {
            FadeType = FadeType.Complete;

            SignalFadedEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void OnSignalStarted()
        {
            SignalStartedEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void StartFade(float volume)
        {
            OnSignalStarted();

            if (fadeSampleCount == 0)
            {
                volumeCurrent = volume;

                OnSignalFaded();
            }
            else
            {
                if (volume > volumeCurrent)
                {
                    FadeType = FadeType.In;
                    volumeFadeStart = volumeCurrent;
                    volumeFadeDiff = (volume - volumeCurrent) / fadeSampleCount;
                }
                else
                {
                    FadeType = FadeType.Out;
                    volumeFadeStart = volumeCurrent;
                    volumeFadeDiff = (volumeCurrent - volume) / fadeSampleCount;
                }

                lock (sourceProvider)
                {
                    fadeSamplePosition = 0;
                }
            }
        }

        #endregion Private Methods
    }
}