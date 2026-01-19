using System;
using NAudio.Dsp;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;

namespace SportDJ.Shareds.SampleProviders.Core
{
    public class FadingProvider
        : BaseProvider
    {
        #region Private Fields

        private readonly object lockProvider = new();
        private readonly Random random;
        private readonly ISampleProvider sourceProvider;

        private long fadeSampleCount;
        private long fadeSamplePosition;
        private BiQuadFilter[] lowPassFilters;
        private float volumeCurrent;
        private float volumeFadeDiff;
        private float volumeFadeStart;

        #endregion Private Fields

        #region Public Constructors

        public FadingProvider(ISampleProvider sourceProvider, float volume = Constants.VolumeMin)
            : base(waveFormat: sourceProvider?.WaveFormat)
        {
            this.sourceProvider = sourceProvider;

            volumeCurrent = volume;
            Volume = volumeCurrent;

            random = new Random();
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
                lock (lockProvider)
                {
                    Volume = volumeTarget;

                    FadeType = volumeTarget > volumeCurrent
                        ? FadeType.Raise
                        : FadeType.Reduce;

                    lowPassFilters = useFilterEffect
                        ? GetFilters()
                        : default;

                    fadeSamplePosition = 0;

                    fadeSampleCount = milliSeconds.HasValue
                        ? (long)(TimeSpan.FromMilliseconds(milliSeconds.Value).TotalSeconds * WaveFormat.SampleRate)
                        : default;

                    if (fadeSampleCount > 0)
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
                    }
                    else
                    {
                        // No fade, set to target directly
                        volumeCurrent = volumeTarget;
                    }
                }
            }

            OnFadingStarted();

            if (fadeSampleCount == 0 || volumeCurrent == volumeTarget)
            {
                OnFadingEnded();
            }
        }

        public void FadeIn(int? milliSeconds = default, bool useFilterEffect = false)
        {
            Fade(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: milliSeconds,
                useFilterEffect: useFilterEffect);
        }

        public void FadeOut(int? milliSeconds = default, bool useFilterEffect = false)
        {
            Fade(
                volumeTarget: Constants.VolumeMin,
                milliSeconds: milliSeconds,
                useFilterEffect: useFilterEffect);
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
                    fadeSamplePosition++;

                    if (volumeCurrent == Volume || fadeSamplePosition > fadeSampleCount)
                    {
                        OnFadingEnded();
                    }
                    else if (fadeSampleCount > 0)
                    {
                        if (FadeType == FadeType.Raise)
                        {
                            volumeCurrent = volumeFadeStart + fadeSamplePosition * volumeFadeDiff;
                        }
                        else if (FadeType == FadeType.Reduce)
                        {
                            volumeCurrent = volumeFadeStart - fadeSamplePosition * volumeFadeDiff;

                            if (lowPassFilters != default)
                            {
                                var fadeSampleProgress = (float)fadeSamplePosition / fadeSampleCount;
                                var fadeSampleFactor = (float)Math.Pow(1 - fadeSampleProgress, 2);

                                var minCutoffFrequency = Math.Max(
                                    val1: Constants.FilterCutoffMax,
                                    val2: Constants.FilterCutoffFrequency * fadeSampleFactor);

                                cutoffFrequency = (float)(random.NextDouble() * 100) + minCutoffFrequency;
                                resonance = 1.0f + (1 - fadeSampleFactor) * 2.0f;
                            }
                        }
                    }
                }

                for (var channel = 0; channel < WaveFormat.Channels; channel++)
                {
                    var sample = offset + channel + index;

                    if (lowPassFilters != default
                        && cutoffFrequency > 0)
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
            lock (lockProvider)
            {
                volumeCurrent = Volume;

                FadeType = FadeType.None;
            }

            FadingEndedEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void OnFadingStarted()
        {
            FadingStartedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}