using System;
using NAudio.Wave;

namespace SportDJ.Tests.SampleProvidersTests.Helpers
{
    internal class SilentProvider(int sampleRate = 44100, int channels = 2)
        : ISampleProvider
    {
        #region Public Properties

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(
            sampleRate: sampleRate,
            channels: channels);

        #endregion Public Properties

        #region Public Methods

        public int Read(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count); // komplette Stille
            return count;
        }

        #endregion Public Methods
    }
}