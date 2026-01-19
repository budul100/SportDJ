using System;
using SportDJ.Commons.Enums;

namespace SportDJ.Shareds.SampleProviders.Core.Extensions
{
    public static class ProviderExtensions
    {
        #region Public Methods

        public static bool IsActive(this PlayType playType)
        {
            return playType.IsRunning() || playType == PlayType.Pausing;
        }

        public static bool IsRunning(this PlayType playType)
        {
            return playType == PlayType.Playing || playType == PlayType.Stopping;
        }

        public static long ToBytes(this TimeSpan duration, int averageBytesPerSecond)
        {
            var result = duration.Seconds * averageBytesPerSecond;

            return result;
        }

        public static TimeSpan ToDuration(this long bytes, int averageBytesPerSecond)
        {
            var seconds = (double)bytes / averageBytesPerSecond;
            var result = seconds >= 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.Zero;

            return result;
        }

        #endregion Public Methods
    }
}