using System;
using System.Collections.Generic;

namespace LoopDetector
{
    public sealed class RmsEnvelopeExtractor
    {
        #region Private Fields

        private readonly int hopSize;
        private readonly float[] window;
        private readonly int windowSize;
        private float sumSquares;
        private int windowPos;

        #endregion Private Fields

        #region Public Constructors

        public RmsEnvelopeExtractor(int windowSize = 1024, int hopSize = 512)
        {
            this.windowSize = windowSize;
            this.hopSize = hopSize;
            window = new float[windowSize];
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Liefert alle neu entstandenen RMS-Werte für diesen Sample-Block
        /// </summary>
        public IEnumerable<float> Process(float[] samples, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = samples[offset + i];

                // Sliding window RMS
                sumSquares -= window[windowPos];
                float sq = x * x;
                window[windowPos] = sq;
                sumSquares += sq;

                windowPos++;
                if (windowPos >= windowSize)
                    windowPos = 0;

                if ((i % hopSize) == 0)
                {
                    yield return (float)Math.Sqrt(sumSquares / windowSize);
                }
            }
        }

        #endregion Public Methods
    }
}