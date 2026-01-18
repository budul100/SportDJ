using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopDetector
{
    public sealed class RealTimeLoopDetector
    {
        #region Private Fields

        private const int EnvelopeRate = SampleRate / 512;
        private const int SampleRate = 44100;
        // ≈86 Hz

        private readonly FloatRingBuffer envelopeHistory =
            new(capacity: EnvelopeRate * 32);

        private readonly RmsEnvelopeExtractor rms = new();

        #endregion Private Fields

        // 32 Sekunden

        #region Public Methods

        public void ProcessAudioBlock(float[] samples, int offset, int count)
        {
            foreach (var env in rms.Process(samples, offset, count))
                envelopeHistory.Push(env);
        }

        public LoopPoints TryDetectLoop(
            float[] allSamples,
            long currentSample)
        {
            int minLag = 2 * EnvelopeRate;
            int maxLag = 30 * EnvelopeRate;

            int lag = EnvelopeAutocorrelator.FindBestLag(
                envelopeHistory, minLag, maxLag);

            if (lag <= 0)
                return new LoopPoints { Confidence = 0 };

            long loopLengthSamples =
                (long)(lag * (double)SampleRate / EnvelopeRate);

            long approxStart = currentSample - loopLengthSamples;
            if (approxStart < 0)
                return new LoopPoints { Confidence = 0 };

            // Feinausrichtung (dein bewährter Code)
            long refinedStart =
                SampleLoopDetector.FindRisingEdgeTransient(
                    allSamples,
                    approxStart - 200,
                    approxStart + 200);

            return new LoopPoints
            {
                StartSample = refinedStart,
                EndSample = currentSample,
                Confidence = 1.0
            };
        }

        #endregion Public Methods
    }
}