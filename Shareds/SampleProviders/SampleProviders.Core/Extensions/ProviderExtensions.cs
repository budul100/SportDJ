using System;
using SportDJ.Commons.Enums;

namespace SportDJ.Shareds.SampleProviders.Core.Extensions;

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

    public static TimeSpan ToDuration(this long samples, int sampleRate)
    {
        var seconds = (double)samples / sampleRate;

        var result = seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.Zero;

        return result;
    }

    public static long ToSamples(this TimeSpan duration, int sampleRate)
    {
        var result = (long)(duration.TotalSeconds * sampleRate);

        return result;
    }

    public static long ToSamples(this long bytes, int bytesPerSample)
    {
        return bytes / bytesPerSample;
    }

    #endregion Public Methods
}