using System;
using NAudio.Wave.SampleProviders;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Shareds.SampleProviders.Core;
using SportDJ.Tests.SampleProvidersTests.Helpers;
using Xunit;

namespace SportDJ.Tests.SampleProvidersTests;

public class FadingProviderTests
{
    #region Private Fields

    private const int FadeMilliseconds = 2000;
    private const int SampleRate = 44100;

    #endregion Private Fields

    #region Public Methods

    [Fact]
    public void FadeOut_SilentSource_InvokesFadingEndedOnRead()
    {
        // Arrange
        var source = new SilentProvider();
        var fadingProvider = new FadingProvider(source, volume: Constants.VolumeMax);

        var endedEventRaised = false;
        fadingProvider.FadingEndedEvent += (_, __) => endedEventRaised = true;

        // Act: start a fade-out (target = VolumeMin) with duration > 0 so fade logic runs
        fadingProvider.FadeOut(milliSeconds: 5000, useFilterEffect: false);

        // We require silence for a certain duration (100 ms) before the fade ends.
        // Calculate how many frames correspond to 100 ms and request that many samples.
        var sampleRate = source.WaveFormat.SampleRate;
        var channels = source.WaveFormat.Channels;
        var framesRequired = (int)Math.Ceiling(sampleRate * (100.0 / 1000.0)); // 100 ms
        var buffer = new float[framesRequired * channels];

        // Read once from the provider, the source provides complete silence,
        // after applying volume the consecutive silent frames should reach the required duration.
        var read = fadingProvider.Read(buffer);

        // Assert
        Assert.Equal(buffer.Length, read); // Read returned samples
        Assert.True(endedEventRaised, "FadingEndedEvent must be raised when the source is silent for the configured silence duration during a fade-out.");
        Assert.Equal(FadeType.None, fadingProvider.FadeType); // Fade was ended
        Assert.True(fadingProvider.IsSilent, "Provider must be silent after fade ended.");
    }

    [Fact]
    public void FadeOut_WithFilterEffect_AttenuatesHighFrequenciesMoreThanWithout()
    {
        // Act
        var rmsFiltered = GetRmsLateInFade(useFilterEffect: true);
        var rmsUnfiltered = GetRmsLateInFade(useFilterEffect: false);

        // Assert: at 75% of the fade the cutoff is around 1.25 kHz, so an 8 kHz tone must be damped heavily
        Assert.True(
            rmsUnfiltered > 0,
            "Reference without filter must not be silent.");
        Assert.True(
            rmsFiltered < rmsUnfiltered * 0.5,
            $"Low-pass sweep has no audible effect (filtered RMS {rmsFiltered:F5}, unfiltered RMS {rmsUnfiltered:F5}).");
    }

    #endregion Public Methods

    #region Private Methods

    private static double GetRmsLateInFade(bool useFilterEffect)
    {
        var source = new SignalGenerator(SampleRate, 2)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = 8000,
            Gain = 1.0
        };

        var fadingProvider = new FadingProvider(source, volume: Constants.VolumeMax);

        fadingProvider.FadeOut(
            milliSeconds: FadeMilliseconds,
            useFilterEffect: useFilterEffect);

        // Skip the first 75% of the fade
        var skipBuffer = new float[(int)(SampleRate * 2 * (FadeMilliseconds / 1000.0) * 0.75)];
        fadingProvider.Read(skipBuffer);

        // Measure the following 100 ms
        var measureBuffer = new float[SampleRate / 10 * 2];
        var samplesRead = fadingProvider.Read(measureBuffer);

        double sum = 0;

        for (var index = 0; index < samplesRead; index++)
        {
            sum += measureBuffer[index] * measureBuffer[index];
        }

        return samplesRead > 0
            ? Math.Sqrt(sum / samplesRead)
            : 0;
    }

    #endregion Private Methods
}
