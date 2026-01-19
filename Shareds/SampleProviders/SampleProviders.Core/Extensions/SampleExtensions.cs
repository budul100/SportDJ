using System;

namespace SportDJ.Shareds.SampleProviders.Core.Extensions
{
    public static class SampleExtensions
    {
        #region Private Fields

        // Resolution of the analysis. 1024 samples @ 44.1kHz ≈ 23ms per step.
        // Good balance between performance and accuracy for long phrases.
        private const int HopSize = 1024;

        // Window size for RMS calculation (smoothing).
        private const int WindowSize = 2048;

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Detects a loop ending near the current position by analyzing the audio history.
        /// </summary>
        /// <param name="allSamples">The full audio buffer.</param>
        /// <param name="position">The current playback cursor position (in samples).</param>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <param name="minLoopDuration">Minimum length of the loop in seconds (default: 10s).</param>
        /// <param name="maxLoopDuration">Maximum length of the loop in seconds (default: 45s).</param>
        /// <returns>A tuple containing Start and End sample positions, or null if no loop found.</returns>
        public static (long? Start, long? End) DetectLoop(this
            float[] allSamples,
            long position,
            int sampleRate,
            double minLoopDuration = 5.0,
            double maxLoopDuration = 20.0)
        {
            // Ensure we have enough history for the minimum requested loop
            var neededHistory = (long)(sampleRate * minLoopDuration);
            if (allSamples == null || position < neededHistory) return (null, null);

            // 1. FIND ANCHOR (Future)
            // We search in the near future (now ... now + 1.5s) for a strong downbeat/transient.
            // This ensures the loop ends cleanly on a beat.
            var refinedEnd = FindStrongestTransient(allSamples, position, (long)(sampleRate * 1.5));

            // Fallback: If no transient found in future, snap to current position
            if (refinedEnd < position) refinedEnd = position;

            // 2. SEARCH PATTERN (Past)
            // We analyze a window far enough back to cover the max loop duration + buffer
            var analysisSeconds = (long)maxLoopDuration + 15;
            var analysisLength = sampleRate * analysisSeconds;
            var startAnalysis = Math.Max(0, refinedEnd - analysisLength);
            var lengthAnalysis = refinedEnd - startAnalysis;

            // Create RMS envelope (energy curve)
            var envelope = CalculateEnvelope(allSamples, startAnalysis, lengthAnalysis);

            var envSampleRate = sampleRate / HopSize;
            var minLagIdx = (int)(minLoopDuration * envSampleRate);
            var maxLagIdx = (int)(maxLoopDuration * envSampleRate);

            // Define the "Fingerprint" size we compare (last 1.5 seconds of the envelope)
            var patternWidth = (int)(1.5 * envSampleRate);

            // Perform the search (Difference Function / Autocorrelation)
            var bestLag = FindBestPatternMatch(envelope, minLagIdx, maxLagIdx, patternWidth);

            if (bestLag <= 0) return (null, null);

            // 3. CALCULATE START
            var loopLengthSamples = (long)bestLag * HopSize;

            // Strict Phase Locking: The start is derived exactly from the End minus Length.
            // We do NOT search for a separate transient peak for the start, to ensure rhythm consistency.
            var exactStart = refinedEnd - loopLengthSamples;

            // 4. FINE TUNING (Anti-Click / Phase Alignment)
            // Find the nearest zero-crossing (rising edge) within +/- 50ms to ensure the waveform matches.
            var refinedStart = SnapToZeroCrossing(allSamples, exactStart, sampleRate / 20);

            // STUMBLE CHECK / PRE-TRANSIENT CORRECTION:
            // Check if we accidentally locked onto the silence/noise *before* the kick.
            // If the signal 15ms later is significantly louder (> 2x), we were too early.
            var checkPos = refinedStart + sampleRate / 70; // check ~14ms ahead

            if (checkPos < allSamples.Length &&
                Math.Abs(allSamples[checkPos]) > Math.Abs(allSamples[refinedStart]) * 2.0)
            {
                // We were in the "pre-roll". Snap forward to the actual loud peak.
                refinedStart = FindRisingEdgeTransient(allSamples, refinedStart, refinedStart + sampleRate / 10);
            }

            // Final Validation
            // Ensure start is valid and the loop isn't impossibly short
            if (refinedStart >= 0 && refinedEnd > refinedStart + sampleRate * minLoopDuration * 0.9)
            {
                return (refinedStart, refinedEnd);
            }

            return (null, null);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Calculates the Root Mean Square (RMS) envelope of the audio signal.
        /// This creates a low-resolution "map" of the song's energy.
        /// </summary>
        private static float[] CalculateEnvelope(float[] samples, long offset, long count)
        {
            var envelopeLength = (int)(count / HopSize);
            var envelope = new float[envelopeLength];
            var envIndex = 0;

            for (long i = 0; i < count - WindowSize; i += HopSize)
            {
                double sum = 0;
                // Optimization: Sampling every 8th value is sufficient for RMS accuracy here
                for (var j = 0; j < WindowSize; j += 8)
                {
                    var val = samples[offset + i + j];
                    sum += val * val;
                }
                envelope[envIndex++] = (float)Math.Sqrt(sum / (WindowSize / 8));
                if (envIndex >= envelopeLength) break;
            }
            return envelope;
        }

        /// <summary>
        /// Finds the best match for the end-pattern in the past using Mean Squared Error (Difference Function).
        /// </summary>
        private static int FindBestPatternMatch(float[] envelope, int minLag, int maxLag, int patternWidth)
        {
            var signalEnd = envelope.Length - 1;

            // Safety check: Is the envelope long enough?
            if (signalEnd - patternWidth < 0) return -1;

            // Limit maxLag to available history
            var searchLimit = envelope.Length - patternWidth;
            if (maxLag > searchLimit) maxLag = searchLimit;

            var minDifference = double.MaxValue;
            var bestLag = -1;

            // Iterate backwards through history within the allowed range
            for (var lag = minLag; lag < maxLag; lag++)
            {
                double currentDiff = 0;

                // Compare the pattern at the end (Future/Now) with the pattern in the past (Now - lag)
                for (var i = 0; i < patternWidth; i++)
                {
                    var patternVal = envelope[signalEnd - i];      // Reference (Target)
                    var pastVal = envelope[signalEnd - i - lag];   // Candidate

                    var diff = patternVal - pastVal;
                    currentDiff += diff * diff;
                }

                // We want the LOWEST difference (best similarity)
                if (currentDiff < minDifference)
                {
                    minDifference = currentDiff;
                    bestLag = lag;
                }
            }

            return bestLag;
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

        /// <summary>
        /// Searches a small radius around a specific point for the nearest Rising Edge Zero-Crossing.
        /// Used for phase alignment without changing the rhythmic feel.
        /// </summary>
        private static long SnapToZeroCrossing(float[] samples, long position, int searchRadius)
        {
            var start = Math.Max(1, position - searchRadius);
            var end = Math.Min(samples.Length - 1, position + searchRadius);

            var bestPos = position;
            var closestDist = long.MaxValue;
            var found = false;

            for (var i = start; i < end; i++)
            {
                // Check for Rising Edge (- to +)
                if (samples[i] >= 0 && samples[i - 1] < 0)
                {
                    var dist = Math.Abs(i - position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestPos = i;
                        found = true;
                    }
                }
            }

            // Fallback: If no rising edge found (e.g., extremely low freq), find lowest amplitude point
            if (!found)
            {
                var minAmp = float.MaxValue;
                for (var i = start; i < end; i++)
                {
                    var amp = Math.Abs(samples[i]);
                    if (amp < minAmp)
                    {
                        minAmp = amp;
                        bestPos = i;
                    }
                }
            }

            return bestPos;
        }

        #endregion Private Methods
    }
}