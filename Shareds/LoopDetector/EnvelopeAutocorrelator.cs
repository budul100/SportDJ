using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopDetector
{
    public static class EnvelopeAutocorrelator
    {
        public static int FindBestLag(
            FloatRingBuffer env,
            int minLag,
            int maxLag)
        {
            double bestScore = 0;
            int bestLag = -1;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                double sum = 0;

                // Korrelation über begrenztes Fenster (z. B. 5 s)
                int window = Math.Min(env.Count - lag, 400);

                for (int i = 0; i < window; i++)
                {
                    sum += env[i] * env[i + lag];
                }

                if (sum > bestScore)
                {
                    bestScore = sum;
                    bestLag = lag;
                }
            }

            return bestLag;
        }
    }

}
