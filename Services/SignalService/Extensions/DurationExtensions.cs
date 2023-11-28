using System;

namespace SportDJ.Services.SignalService.Extensions
{
    internal static class DurationExtensions
    {
        #region Public Methods

        public static long ToLength(this TimeSpan? time, int averageBytesPerSecond)
        {
            var result = time.HasValue
                ? (long)(time.Value.TotalMilliseconds * averageBytesPerSecond / 1000)
                : default;

            return result;
        }

        public static TimeSpan ToTime(this long size, int averageBytesPerSecond)
        {
            var value = (double)size * 1000 / averageBytesPerSecond;
            var result = TimeSpan.FromMilliseconds(value);

            return result;
        }

        #endregion Public Methods
    }
}