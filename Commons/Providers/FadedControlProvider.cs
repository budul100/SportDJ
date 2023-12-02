using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Extensions;
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
                Volume = volume;

                fadeSampleCount = duration.HasValue
                    ? (long)(duration.Value.TotalSeconds * averageBytesPerSecond)
                    : default;
            }

            StartFade(Volume);
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
                    OnSignalFaded();
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

        private void OnSignalFaded()
        {
            FadeType = FadeType.Complete;
            volumeCurrent = Volume;

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
                    FadeType = FadeType.Raise;
                    volumeFadeStart = volumeCurrent;
                    volumeFadeDiff = (volume - volumeCurrent) / fadeSampleCount;
                }
                else if (volume < volumeCurrent)
                {
                    FadeType = FadeType.Reduce;
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