using System;

namespace SportDJ.Shareds.SampleProviders.Core.Extensions
{
    public static class SampleExtensions
    {
        #region Private Fields

        // Resolution of the energy envelope. 512 samples @ 44.1kHz ≈ 11.6ms per step.
        private const int HopSize = 512;

        // Minimum waveform similarity (0..1) of the loop seam. Below it, no loop is reported.
        private const double MinSeamQuality = 0.35;

        // Minimum similarity (0..1) of the envelope pattern. Below it, the track has no repeating structure.
        private const double MinStructureScore = 0.5;

        // Length of the pattern that is compared against the past. It has to cover a full harmonic phrase.
        // With a short pattern (1.5s), every multiple of the beat matches, because the drums repeat
        // every bar, and the loop ends up in the middle of a chord progression.
        private const double PatternSeconds = 8.0;

        // Lags with a score within this distance of the best score are treated as equally good.
        // The shortest of them wins (shortest repeating unit).
        private const double ScoreTolerance = 0.03;

        // Window size for RMS calculation (smoothing).
        private const int WindowSize = 1024;

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Detects a loop ending near the current position by analyzing the audio history.
        /// The loop length is searched on a coarse energy envelope (musical structure) and then
        /// refined on the waveform (sample accurate), so the jump from end to start is seamless.
        /// </summary>
        /// <param name="allSamples">The full mono audio buffer. Must be decoded the same way as the played audio.</param>
        /// <param name="position">The current playback cursor position (in frames).</param>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <param name="minLoopDuration">Minimum length of the loop in seconds (default: 5s).</param>
        /// <param name="maxLoopDuration">Maximum length of the loop in seconds (default: 20s).</param>
        /// <returns>A tuple containing Start and End frame positions, or null if no loop found.</returns>
        public static (long? Start, long? End) DetectLoop(this
            float[] allSamples,
            long position,
            int sampleRate,
            double minLoopDuration = 5.0,
            double maxLoopDuration = 20.0)
        {
            // Ensure we have enough history for the minimum requested loop
            var neededHistory = (long)(sampleRate * minLoopDuration);

            if (allSamples == null
                || position < neededHistory
                || position >= allSamples.Length)
            {
                return (null, null);
            }

            // 1. FIND ANCHOR (Future)
            // We search in the near future (now ... now + 1.5s) for a strong transient.
            // This ensures the loop ends cleanly on a beat.
            var end = FindStrongestTransient(allSamples, position, (long)(sampleRate * 1.5));

            if (end < position)
            {
                end = position;
            }

            // 2. ENERGY ENVELOPE (Past)
            // Far enough back to cover the maximum loop plus the pattern that is compared.
            var analysisLength = (long)(sampleRate * (maxLoopDuration + PatternSeconds + 1));
            var startAnalysis = Math.Max(0, end - analysisLength);
            var lengthAnalysis = Math.Min(end, allSamples.Length) - startAnalysis;

            var envelope = CalculateEnvelope(allSamples, startAnalysis, lengthAnalysis);

            var envelopeRate = (double)sampleRate / HopSize;
            var minLag = (int)(minLoopDuration * envelopeRate);
            var maxLag = (int)(maxLoopDuration * envelopeRate);

            // With a short history the pattern shrinks, but it must stay long enough to be meaningful
            var patternWidth = Math.Min(
                val1: (int)(PatternSeconds * envelopeRate),
                val2: envelope.Length - minLag - 1);

            if (patternWidth < (int)(2.0 * envelopeRate))
            {
                return (null, null);
            }

            maxLag = Math.Min(maxLag, envelope.Length - patternWidth);

            if (maxLag <= minLag + 1)
            {
                return (null, null);
            }

            // 3. COARSE LOOP LENGTH
            var scores = CalculateScores(envelope, minLag, maxLag, patternWidth);
            var bestLag = SelectLag(scores, minLag, maxLag);

            if (bestLag < 0)
            {
                return (null, null);
            }

            // 4. FINE TUNING (Sample accuracy)
            // First +/- 2 envelope steps with every 4th sample, then +/- 4 samples exactly.
            // The first difference of the signal emphasizes transients (timing) over the bass (phase).
            var before = (int)(0.2 * sampleRate);
            var after = (int)(0.3 * sampleRate);

            var loopLength = RefineLength(
                samples: allSamples,
                end: end,
                length: (long)bestLag * HopSize,
                radius: 2 * HopSize,
                before: before,
                after: after,
                step: 4,
                quality: out _);

            loopLength = RefineLength(
                samples: allSamples,
                end: end,
                length: loopLength,
                radius: 4,
                before: before,
                after: after,
                step: 1,
                quality: out var quality);

            // Strict Phase Locking: The start is derived exactly from the End minus Length.
            var start = end - loopLength;

            // Final Validation
            // Ensure start is valid, the seam matches and the loop isn't impossibly short
            if (start >= 0
                && quality >= MinSeamQuality
                && loopLength >= sampleRate * minLoopDuration * 0.9)
            {
                return (start, end);
            }

            return (null, null);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Calculates the Root Mean Square (RMS) envelope of the audio signal.
        /// This creates a low-resolution "map" of the song's energy. The square root compresses
        /// the dynamics, so loud kicks do not dominate the comparison.
        /// </summary>
        private static double[] CalculateEnvelope(float[] samples, long offset, long count)
        {
            var envelopeLength = (int)((count - WindowSize) / HopSize) + 1;

            if (envelopeLength <= 0)
            {
                return [];
            }

            var envelope = new double[envelopeLength];

            for (var index = 0; index < envelopeLength; index++)
            {
                var windowStart = offset + (long)index * HopSize;

                double sum = 0;

                // Optimization: Sampling every 8th value is sufficient for RMS accuracy here
                for (var j = 0; j < WindowSize; j += 8)
                {
                    var value = samples[windowStart + j];
                    sum += value * value;
                }

                envelope[index] = Math.Sqrt(Math.Sqrt(sum / (WindowSize / 8)));
            }

            return envelope;
        }

        /// <summary>
        /// Pearson correlation of the envelope pattern at the end with the pattern "lag" steps earlier.
        /// </summary>
        private static double[] CalculateScores(double[] envelope, int minLag, int maxLag, int patternWidth)
        {
            var scores = new double[maxLag + 1];

            Array.Fill(scores, -1.0);

            var referenceStart = envelope.Length - patternWidth;

            for (var lag = minLag; lag <= maxLag; lag++)
            {
                scores[lag] = CorrelateEnvelope(
                    envelope: envelope,
                    startA: referenceStart,
                    startB: referenceStart - lag,
                    length: patternWidth);
            }

            return scores;
        }

        private static double CorrelateEnvelope(double[] envelope, int startA, int startB, int length)
        {
            double meanA = 0;
            double meanB = 0;

            for (var i = 0; i < length; i++)
            {
                meanA += envelope[startA + i];
                meanB += envelope[startB + i];
            }

            meanA /= length;
            meanB /= length;

            double sumAB = 0;
            double sumAA = 0;
            double sumBB = 0;

            for (var i = 0; i < length; i++)
            {
                var a = envelope[startA + i] - meanA;
                var b = envelope[startB + i] - meanB;

                sumAB += a * b;
                sumAA += a * a;
                sumBB += b * b;
            }

            return sumAA > 1e-12 && sumBB > 1e-12
                ? sumAB / Math.Sqrt(sumAA * sumBB)
                : -1.0;
        }

        /// <summary>
        /// Normalized cross correlation of the first difference of two segments.
        /// </summary>
        private static double CorrelateDifference(float[] samples, long startA, long startB, long length, int step)
        {
            double sumAB = 0;
            double sumAA = 0;
            double sumBB = 0;

            for (long i = 0; i < length; i += step)
            {
                var a = GetDifference(samples, startA + i);
                var b = GetDifference(samples, startB + i);

                sumAB += a * b;
                sumAA += a * a;
                sumBB += b * b;
            }

            return sumAA > 1e-12 && sumBB > 1e-12
                ? sumAB / Math.Sqrt(sumAA * sumBB)
                : -1.0;
        }

        /// <summary>
        /// Finds the loudest peak in the range and returns the Zero-Crossing (Rising Edge) immediately preceding it.
        /// This ensures cuts are made exactly when the waveform crosses 0 upwards (snappy, no clicks).
        /// </summary>
        private static long FindRisingEdgeTransient(float[] samples, long start, long end)
        {
            if (start < 0) start = 0;
            if (end > samples.Length) end = samples.Length;
            if (start >= end) return start;

            // A. Find the loudest peak
            float maxVol = -1;
            var peakPos = start;

            for (var i = start; i < end; i++)
            {
                var vol = Math.Abs(samples[i]);
                if (vol > maxVol)
                {
                    maxVol = vol;
                    peakPos = i;
                }
            }

            // If silent, return start
            if (maxVol < 0.001f) return start;

            // B. Rewind from peak to the nearest Zero-Crossing (Rising Edge: - to +)
            var cursor = peakPos;
            var limit = Math.Max(start, peakPos - 2500); // Look back max ~50ms

            while (cursor > limit)
            {
                if (cursor > 0 && samples[cursor] >= 0 && samples[cursor - 1] < 0)
                {
                    return cursor;
                }
                cursor--;
            }

            // Fallback: Return peak position if no cleaner cut found
            return peakPos;
        }

        /// <summary>
        /// Alias for FindRisingEdgeTransient to maintain consistent naming.
        /// </summary>
        private static long FindStrongestTransient(float[] samples, long start, long length)
        {
            return FindRisingEdgeTransient(samples, start, start + length);
        }

        private static double GetDifference(float[] samples, long index)
        {
            return index > 0
                ? samples[index] - samples[index - 1]
                : 0;
        }

        /// <summary>
        /// Searches the length around the coarse value, where the waveform around the end
        /// matches the waveform around the (end - length) best.
        /// </summary>
        private static long RefineLength(float[] samples, long end, long length, int radius, int before,
            int after, int step, out double quality)
        {
            var bestLength = length;
            quality = -2.0;

            var windowStart = end - before;
            var windowEnd = Math.Min(samples.Length, end + after);

            for (var candidate = length - radius; candidate <= length + radius; candidate += step)
            {
                var shifted = windowStart - candidate;

                if (shifted < 1)
                {
                    continue;
                }

                var similarity = CorrelateDifference(
                    samples: samples,
                    startA: windowStart,
                    startB: shifted,
                    length: windowEnd - windowStart,
                    step: step);

                if (similarity > quality)
                {
                    quality = similarity;
                    bestLength = candidate;
                }
            }

            return bestLength;
        }

        /// <summary>
        /// Picks the shortest local maximum that is almost as good as the best one.
        /// Multiples of the shortest repeating unit score equally well, but shorter loops are preferred.
        /// </summary>
        private static int SelectLag(double[] scores, int minLag, int maxLag)
        {
            var best = double.MinValue;
            var bestLag = -1;

            for (var lag = minLag; lag <= maxLag; lag++)
            {
                if (scores[lag] > best)
                {
                    best = scores[lag];
                    bestLag = lag;
                }
            }

            if (best < MinStructureScore)
            {
                return -1;
            }

            for (var lag = minLag + 1; lag < maxLag; lag++)
            {
                if (scores[lag] >= scores[lag - 1]
                    && scores[lag] >= scores[lag + 1]
                    && scores[lag] >= best - ScoreTolerance)
                {
                    return lag;
                }
            }

            return bestLag;
        }

        #endregion Private Methods
    }
}
