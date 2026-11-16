using System;
using System.IO;
using NAudio.Wave;

namespace SportDJ.Tests.SignalServiceTests.Helpers;

internal static class WavFileFactory
{
    #region Public Methods

    /// <summary>
    /// Writes a silent 16 bit stereo WAV file with the given duration into the temp folder.
    /// </summary>
    public static string CreateSilent(TimeSpan duration, int sampleRate = 44100)
    {
        var path = Path.Combine(Path.GetTempPath(), $"SportDJ_{Guid.NewGuid():N}.wav");

        var format = new WaveFormat(
            rate: sampleRate,
            bits: 16,
            channels: 2);

        using var writer = new WaveFileWriter(path, format);

        var bytes = new byte[format.BlockAlign * (int)(sampleRate * duration.TotalSeconds)];

        writer.Write(bytes, 0, bytes.Length);

        return path;
    }

    #endregion Public Methods
}
