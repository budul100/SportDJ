using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Shareds.SampleProviders.Core;
using SportDJ.Tests.SampleProvidersTests.Helpers;
using Xunit;

namespace SportDJ.Tests.SampleProvidersTests
{
    public class FadingProviderTests
    {
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
            var framesRequired = (int)System.Math.Ceiling(sampleRate * (100.0 / 1000.0)); // 100 ms
            var buffer = new float[framesRequired * channels];

            // Read once from the provider — the source provides complete silence,
            // after applying volume the consecutive silent frames should reach the required duration.
            var read = fadingProvider.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(buffer.Length, read); // Read returned samples
            Assert.True(endedEventRaised, "FadingEndedEvent must be raised when the source is silent for the configured silence duration during a fade-out.");
            Assert.Equal(FadeType.None, fadingProvider.FadeType); // Fade was ended
            Assert.True(fadingProvider.IsSilent, "Provider must be silent after fade ended.");
        }

        #endregion Public Methods
    }
}