using System;
using System.Threading;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Shareds.SampleProviders.Core.Filters;

namespace SportDJ.Shareds.SampleProviders.Core;

public class FadingProvider
    : BaseProvider
{
    #region Private Fields

    // Continuous silence duration required to consider stream silent and end the fade.
    private const int SilenceDurationMilliseconds = 100;

    // Threshold for detecting silence (after applying volume)
    private const float SilenceThreshold = 1e-4f;

    private int consecutiveSilentFrames;
    private long fadeSampleCount;
    private long fadeSamplePosition;

    private readonly Lock lockProvider = new();
    private readonly int silenceFramesRequired;
    private readonly ISampleProvider sourceProvider;
    private SweepFilter sweepFilter;
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

        silenceFramesRequired = (int)Math.Ceiling(WaveFormat.SampleRate *
            (SilenceDurationMilliseconds / 1000.0));
        consecutiveSilentFrames = 0;
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

                sweepFilter = useFilterEffect
                    ? GetFilter()
                    : default;

                fadeSamplePosition = 0;
                consecutiveSilentFrames = 0;

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

    public override int Read(Span<float> buffer)
    {
        var samplesRead = sourceProvider.Read(buffer);

        lock (lockProvider)
        {
            AdjustSamples(
                buffer: buffer[..samplesRead]);
        }

        return samplesRead;
    }

    #endregion Public Methods

    #region Private Methods

    private void AdjustSamples(Span<float> buffer)
    {
        for (var index = 0; index < buffer.Length; index += WaveFormat.Channels)
        {
            var applyFilter = false;

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

                        if (sweepFilter != default)
                        {
                            sweepFilter.BeginFrame(
                                progress: (double)fadeSamplePosition / fadeSampleCount);

                            applyFilter = true;
                        }
                    }
                }
            }

            var frameOverThreshold = false;

            for (var channel = 0; channel < WaveFormat.Channels; channel++)
            {
                var sample = channel + index;

                if (applyFilter)
                {
                    buffer[sample] = sweepFilter.Transform(
                        channel: channel,
                        input: buffer[sample]);
                }

                buffer[sample] *= volumeCurrent;

                if (Math.Abs(buffer[sample]) > SilenceThreshold)
                {
                    frameOverThreshold = true;
                }
            }

            if (!frameOverThreshold
                && FadeType == FadeType.Reduce
                && Volume == Constants.VolumeMin)
            {
                consecutiveSilentFrames++;

                if (consecutiveSilentFrames >= silenceFramesRequired)
                {
                    OnFadingEnded();
                }
            }
            else
            {
                consecutiveSilentFrames = 0;
            }
        }
    }

    private SweepFilter GetFilter()
    {
        return new SweepFilter(
            sampleRate: WaveFormat.SampleRate,
            channels: WaveFormat.Channels,
            startCutoffFrequency: Constants.FilterCutoffFrequency);
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