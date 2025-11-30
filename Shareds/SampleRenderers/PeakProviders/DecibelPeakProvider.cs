using System;
using NAudio.Wave;

namespace NAudio.WaveFormRenderer.PeakProviders
{
    internal class DecibelPeakProvider 
        : IPeakProvider
    {
        #region Private Fields

        private readonly double dynamicRange;
        private readonly IPeakProvider sourceProvider;

        #endregion Private Fields

        #region Public Constructors

        public DecibelPeakProvider(IPeakProvider sourceProvider, double dynamicRange)
        {
            this.sourceProvider = sourceProvider;
            this.dynamicRange = dynamicRange;
        }

        #endregion Public Constructors

        #region Public Methods

        public PeakInfo GetNextPeak()
        {
            var peak = sourceProvider.GetNextPeak();
            var decibelMax = 20 * Math.Log10(peak.Max);
            if (decibelMax < 0 - dynamicRange) decibelMax = 0 - dynamicRange;
            var linear = (float)((dynamicRange + decibelMax) / dynamicRange);
            return new PeakInfo(0 - linear, linear);
        }

        public void Init(ISampleProvider reader, int samplesPerPixel)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}