using NAudio.Wave;
using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Providers
{
    public class FadedControlProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly ISampleProvider sourceProvider;

        private long fadeSampleCount;
        private long fadeSamplePosition;
        private float volumeCurrent;
        private float volumeFadeDiff;
        private float volumeFadeStart;

        #endregion Private Fields

        #region Public Constructors

        public FadedControlProvider(ISampleProvider sourceProvider, float volume = Constants.VolumeMin)
            : base(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            Volume = volume;
            volumeCurrent = Volume;
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler FadingEndedEvent;

        public event EventHandler FadingStartedEvent;

        #endregion Public Events

        #region Public Properties

        public FadeType FadeType { get; private set; }

        public bool IsSilent => Volume == Constants.VolumeMin;

        public float Volume { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Fade(float volumeTarget, int milliSeconds)
        {
            var duration = TimeSpan.FromMilliseconds(milliSeconds);

            Fade(
                volumeTarget: volumeTarget,
                duration: duration);
        }

        public void Fade(float volumeTarget, TimeSpan? duration = default)
        {
            if (volumeTarget < Constants.VolumeMin || volumeTarget > Constants.VolumeMax)
            {
                throw new ArgumentOutOfRangeException(nameof(volumeTarget));
            }

            if (volumeTarget != Volume)
            {
                FadeType = volumeTarget > volumeCurrent
                    ? FadeType.Raise
                    : FadeType.Reduce;

                fadeSampleCount = duration.HasValue
                    ? (long)(duration.Value.TotalSeconds * averageBytesPerSecond)
                    : default;

                Volume = volumeTarget;

                OnFadingStarted();
            }

            if (fadeSampleCount == 0 || volumeCurrent == Volume)
            {
                OnFadingEnded();
            }
            else
            {
                lock (sourceProvider)
                {
                    if (FadeType == FadeType.Raise)
                    {
                        volumeFadeStart = volumeCurrent;
                        volumeFadeDiff = (Volume - volumeCurrent) / fadeSampleCount;
                    }
                    else if (FadeType == FadeType.Reduce)
                    {
                        volumeFadeStart = volumeCurrent;
                        volumeFadeDiff = (volumeCurrent - Volume) / fadeSampleCount;
                    }

                    fadeSamplePosition = 0;
                }
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
                if (FadeType != FadeType.Complete
                    && (volumeCurrent == Volume || fadeSamplePosition > fadeSampleCount))
                {
                    OnFadingEnded();
                }

                if (FadeType == FadeType.Raise)
                {
                    volumeCurrent = volumeFadeStart + (fadeSamplePosition * volumeFadeDiff);
                }
                else if (FadeType == FadeType.Reduce)
                {
                    volumeCurrent = volumeFadeStart - (fadeSamplePosition * volumeFadeDiff);
                }

                for (var channel = 0; channel < sourceProvider.WaveFormat.Channels; channel++)
                {
                    buffer[offset + sample++] *= volumeCurrent;
                }

                fadeSamplePosition++;
            }
        }

        private void OnFadingEnded()
        {
            volumeCurrent = Volume;

            if (FadeType != FadeType.Complete)
            {
                FadeType = FadeType.Complete;

                FadingStartedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        private void OnFadingStarted()
        {
            FadingEndedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}