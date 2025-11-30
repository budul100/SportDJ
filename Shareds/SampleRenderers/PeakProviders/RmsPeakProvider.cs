using System;

namespace NAudio.WaveFormRenderer.PeakProviders
{
    public class RmsPeakProvider 
        : PeakProvider
    {
        #region Private Fields

        private readonly int blockSize;

        #endregion Private Fields

        #region Public Constructors

        public RmsPeakProvider(int blockSize)
        {
            this.blockSize = blockSize;
        }

        #endregion Public Constructors

        #region Public Methods

        public override PeakInfo GetNextPeak()
        {
            var samplesRead = Provider.Read(ReadBuffer, 0, ReadBuffer.Length);

            var max = 0.0f;
            for (var x = 0; x < samplesRead; x += blockSize)
            {
                var total = 0.0;
                for (var y = 0; y < blockSize && x + y < samplesRead; y++)
                {
                    total += ReadBuffer[x + y] * ReadBuffer[x + y];
                }
                var rms = (float)Math.Sqrt(total / blockSize);

                max = Math.Max(max, rms);
            }

            return new PeakInfo(0 - max, max);
        }

        #endregion Public Methods
    }
}