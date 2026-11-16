using System;
using System.Linq;

namespace NAudio.WaveFormRenderer.PeakProviders
{
    public class AveragePeakProvider 
        : PeakProvider
    {
        #region Private Fields

        private readonly float scale;

        #endregion Private Fields

        #region Public Constructors

        public AveragePeakProvider(float scale)
        {
            this.scale = scale;
        }

        #endregion Public Constructors

        #region Public Methods

        public override PeakInfo GetNextPeak()
        {
            var samplesRead = Provider.Read(ReadBuffer);
            var sum = samplesRead == 0 ? 0 : ReadBuffer.Take(samplesRead).Select(s => Math.Abs(s)).Sum();
            var average = samplesRead == 0 ? 0 : sum / samplesRead;

            return new PeakInfo(average * (0 - scale), average * scale);
        }

        #endregion Public Methods
    }
}
