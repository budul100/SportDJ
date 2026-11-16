using NAudio.Wave;
using System;

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

        public int Read(Span<float> buffer)
        {
            buffer.Clear(); // complete silence

            return buffer.Length;
        }

        #endregion Public Methods
    }
}