using System;
using NAudio.Wave;

namespace SportDJ.Commons.Providers.Extensions
{
    internal static class ProviderExtensions
    {
        #region Public Methods

        public static WaveFormat GetMono(this ISampleProvider provider)
        {
            var result = WaveFormat.CreateIeeeFloatWaveFormat(
                sampleRate: provider.WaveFormat.SampleRate,
                channels: 1);

            return result;
        }

        public static WaveFormat GetStereo(this ISampleProvider provider)
        {
            var result = WaveFormat.CreateIeeeFloatWaveFormat(
                sampleRate: provider.WaveFormat.SampleRate,
                channels: 2);

            return result;
        }

        public static long ToBytes(this TimeSpan duration, int averageBytesPerSecond)
        {
            var result = duration.Seconds * averageBytesPerSecond;

            return result;
        }

        public static TimeSpan ToDuration(this long bytes, int averageBytesPerSecond)
        {
            var seconds = (double)bytes / averageBytesPerSecond;
            var result = TimeSpan.FromSeconds(seconds);

            return result;
        }

        #endregion Public Methods
    }
}