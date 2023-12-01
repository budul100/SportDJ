using NAudio.Wave;
using System;

namespace SportDJ.Commons.Extensions
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

        public static TimeSpan ToDuration(this long seconds, int averageBytesPerSecond)
        {
            var value = (double)seconds * 1000 / averageBytesPerSecond;
            var result = TimeSpan.FromMilliseconds(value);

            return result;
        }

        public static TimeSpan ToDuration(this int milliSeconds)
        {
            var value = (double)milliSeconds;
            var result = TimeSpan.FromMilliseconds(value);

            return result;
        }

        public static long ToSeconds(this TimeSpan? time, int averageBytesPerSecond)
        {
            var result = time.HasValue
                ? (long)(time.Value.TotalMilliseconds * averageBytesPerSecond / 1000)
                : default;

            return result;
        }

        #endregion Public Methods
    }
}