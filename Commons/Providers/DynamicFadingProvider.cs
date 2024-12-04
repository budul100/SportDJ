using System;
using NAudio.Dsp;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Providers
{
    public class DynamicFadingProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly object lockProvider = new();
        private readonly BiQuadFilter[] lowPassFilters;
        private readonly Random random;
        private readonly ISampleProvider sourceProvider;

        private long fadeSampleCount;
        private long fadeSamplePosition;
        private bool useFilterEffect;
        private float volumeCurrent;
        private float volumeFadeDiff;
        private float volumeFadeStart;

        #endregion Private Fields

        #region Public Constructors

        public DynamicFadingProvider(ISampleProvider sourceProvider, float volume = Constants.VolumeMin)
            : base(waveFormat: sourceProvider?.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            volumeCurrent = volume;
            Volume = volumeCurrent;

            random = new Random();
            lowPassFilters = GetFilters();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler FadingEndedEvent;

        public event EventHandler FadingStartedEvent;

        #endregion Public Events

        #region Public Properties

        public FadeType FadeType { get; private set; }

        public bool IsSilent => volumeCurrent == Constants.VolumeMin;

        public float Volume { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Fade(float volumeTarget, int? milliSeconds = default, bool useFilterEffect = false)
        {
            if (volumeTarget < Constants.VolumeMin || volumeTarget > Constants.VolumeMax)
            {
                throw new ArgumentOutOfRangeException(nameof(volumeTarget));
            }

            if (volumeTarget != volumeCurrent)
            {
                FadeType = volumeTarget > volumeCurrent
                    ? FadeType.Raise
                    : FadeType.Reduce;

                Volume = volumeTarget;

                this.useFilterEffect = useFilterEffect;

                fadeSampleCount = milliSeconds.HasValue
                    ? (long)(TimeSpan.FromMilliseconds(milliSeconds.Value).TotalSeconds * averageBytesPerSecond)
                    : default;

                FadingStartedEvent?.Invoke(
                    sender: this,
                    e: default);
            }

            if (fadeSampleCount == 0 || volumeCurrent == volumeTarget)
            {
                OnFadingEnded();
            }
            else
            {
                lock (lockProvider)
                {
                    if (FadeType == FadeType.Raise)
                    {
                        volumeFadeStart = volumeCurrent;
                        volumeFadeDiff = (volumeTarget - volumeCurrent) / fadeSampleCount;
                    }
                    else if (FadeType == FadeType.Reduce)
                    {
                        volumeFadeStart = volumeCurrent;
                        volumeFadeDiff = (volumeCurrent - volumeTarget) / fadeSampleCount;
                    }

                    fadeSamplePosition = 0;
                }
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = sourceProvider.Read(
                buffer: buffer,
                offset: offset,
                count: count);

            lock (lockProvider)
            {
                AdjustSamples(
                    buffer: buffer,
                    offset: offset,
                    count: samplesRead);
            }

            return samplesRead;
        }

        #endregion Public Methods

        #region Private Methods

        private void AdjustSamples(float[] buffer, int offset, int count)
        {
            for (var index = 0; index < count; index += WaveFormat.Channels)
            {
                var cutoffFrequency = default(float);
                var resonance = default(float);

                if (FadeType != FadeType.None)
                {
                    fadeSamplePosition += WaveFormat.BlockAlign;

                    if (volumeCurrent == Volume || fadeSamplePosition > fadeSampleCount)
                    {
                        OnFadingEnded();
                    }
                    else if (FadeType == FadeType.Raise)
                    {
                        volumeCurrent = volumeFadeStart + (fadeSamplePosition * volumeFadeDiff);
                    }
                    else if (FadeType == FadeType.Reduce)
                    {
                        volumeCurrent = volumeFadeStart - (fadeSamplePosition * volumeFadeDiff);
                    }

                    if (useFilterEffect)
                    {
                        var fadeSampleProgress = (float)fadeSamplePosition / fadeSampleCount;
                        var fadeSampleFactor = (float)Math.Pow(1 - fadeSampleProgress, 2); // Exponential fade for smoothness

                        cutoffFrequency = Math.Max(500, Constants.FilterCutoffFrequency * fadeSampleFactor) +
                            (float)(random.NextDouble() * 100);
                        resonance = 1.0f + ((1 - fadeSampleFactor) * 2.0f);
                    }
                }

                for (var channel = 0; channel < WaveFormat.Channels; channel++)
                {
                    var sample = offset + channel + index;

                    if (useFilterEffect)
                    {
                        lowPassFilters[channel].SetLowPassFilter(
                            sampleRate: WaveFormat.SampleRate,
                            cutoffFrequency: cutoffFrequency,
                            q: resonance);

                        buffer[sample] = lowPassFilters[channel].Transform(buffer[sample]);
                    }

                    buffer[sample] *= volumeCurrent;
                }
            }
        }

        private BiQuadFilter[] GetFilters()
        {
            var result = new BiQuadFilter[WaveFormat.Channels];

            for (var index = 0; index < WaveFormat.Channels; index++)
            {
                result[index] = BiQuadFilter.LowPassFilter(
                    sampleRate: WaveFormat.SampleRate,
                    cutoffFrequency: Constants.FilterCutoffFrequency,
                    q: 1.0f);
            }

            return result;
        }

        private void OnFadingEnded()
        {
            volumeCurrent = Volume;

            if (FadeType != FadeType.None)
            {
                FadeType = FadeType.None;

                FadingEndedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        #endregion Private Methods
    }
}