using System;

namespace NAudio.WaveFormRenderer.PeakProviders
{
    public class SamplingPeakProvider
        : PeakProvider
    {
        #region Private Fields

        private readonly int sampleInterval;

        #endregion Private Fields

        #region Public Constructors

        public SamplingPeakProvider(int sampleInterval)
        {
            this.sampleInterval = sampleInterval;
        }

        #endregion Public Constructors

        #region Public Methods

        public override PeakInfo GetNextPeak()
        {
            var samplesRead = Provider.Read(ReadBuffer);
            var max = 0.0f;
            var min = 0.0f;
            for (var x = 0; x < samplesRead; x += sampleInterval)
            {
                max = Math.Max(max, ReadBuffer[x]);
                min = Math.Min(min, ReadBuffer[x]);
            }

            return new PeakInfo(min, max);
        }

        #endregion Public Methods
    }
}