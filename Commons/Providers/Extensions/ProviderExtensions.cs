using System;
using NAudio.Wave;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Providers.Extensions
{
    internal static class ProviderExtensions
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
            var result = TimeSpan.FromSeconds(seconds);

            return result;
        }

        #endregion Public Methods
    }
}