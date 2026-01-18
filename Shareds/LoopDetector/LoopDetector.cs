using System;

namespace LoopDetector
{
    public class SampleLoopDetector
    {
        #region Private Fields

        private const int AnalysisSampleRate = 44100;

        #endregion Private Fields

        // Wir gehen von Standard-Audio aus

        #region Public Methods

        public static LoopPoints FindQuantizedLoopBackwards(float[] allSamples, long currentPositionSample,
            double minLoopSeconds = 2.0)
        {
            // --- SCHRITT 1: Loop-Ende finden (Dein Klick korrigieren) ---

            // Wir suchen +/- 200ms um deinen Klick herum nach dem lautesten Transienten (Beat).
            // Das erlaubt dir, leicht zu früh oder zu spät zu klicken.
            int searchWindow = (int)(AnalysisSampleRate * 0.2);
            long searchStart = Math.Max(0, currentPositionSample - searchWindow);
            long searchEnd = Math.Min(allSamples.Length, currentPositionSample + searchWindow);

            // Finde den Beat (Nulldurchgang der steigenden Flanke)
            long loopEndSample = FindRisingEdgeTransient(allSamples, searchStart, searchEnd);

            // --- SCHRITT 2: Loop-Start suchen (Rückwärts) ---

            // Muster definieren (Der Sound des Beats)
            int patternSize = (int)(AnalysisSampleRate * 0.15); // 150ms
            if (loopEndSample < patternSize || loopEndSample + patternSize >= allSamples.Length)
                return new LoopPoints { Confidence = 0 };

            float[] endPattern = new float[patternSize];
            Array.Copy(allSamples, loopEndSample, endPattern, 0, patternSize);

            // Wir suchen weit zurück (bis 30 Sekunden)
            long minOffset = (long)(minLoopSeconds * AnalysisSampleRate);
            long maxOffset = (long)(30.0 * AnalysisSampleRate); // Max 30 Sek Loop

            long scanEnd = Math.Max(0, loopEndSample - minOffset);
            long scanStart = Math.Max(0, loopEndSample - maxOffset);

            double bestScore = -1;
            long bestStartSample = -1;

            // Step 20 ist schnell und präzise genug für die Grobsuche
            for (long i = scanEnd; i > scanStart; i -= 20)
            {
                double correlation = CalculateCorrelation(endPattern, allSamples, (int)i);

                if (correlation > 0.5)
                {
                    // Längen-Bonus: Wir bevorzugen längere Loops (z.B. 4 Takte statt 1)
                    // Formel: +5% Score pro Sekunde Abstand
                    double seconds = (loopEndSample - i) / (double)AnalysisSampleRate;
                    double lengthBonus = 1.0 + (seconds * 0.05);

                    double finalScore = correlation * lengthBonus;

                    if (finalScore > bestScore)
                    {
                        bestScore = finalScore;
                        bestStartSample = i;
                    }
                }
            }

            // --- SCHRITT 3: Feinsuche (Phase Alignment) ---
            if (bestStartSample != -1)
            {
                // Suche +/- 100 Samples um den Kandidaten nach dem perfekten Rising Edge Zero Crossing
                // Wir nehmen NICHT die Korrelation, sondern richten uns strikt am Nulldurchgang aus.
                // Das verhindert den "Doppel-Schlag" Effekt.

                long refinedStart = FindRisingEdgeTransient(allSamples, bestStartSample - 100, bestStartSample + 100);

                // Nochmal kurz checken, ob das Pattern da wirklich passt
                double confirmScore = CalculateCorrelation(endPattern, allSamples, (int)refinedStart);

                return new LoopPoints
                {
                    StartSample = refinedStart,
                    EndSample = loopEndSample,
                    Confidence = confirmScore
                };
            }

            return new LoopPoints { Confidence = 0 };
        }

        #endregion Public Methods

        #region Private Methods

        private static double CalculateCorrelation(float[] pattern, float[] searchArea, int offset)
        {
            if (offset < 0 || offset + pattern.Length > searchArea.Length) return -1;

            double errorSum = 0;
            // Wir prüfen nur jedes 2. Sample für Speed, reicht völlig
            for (int i = 0; i < pattern.Length; i += 2)
            {
                float diff = pattern[i] - searchArea[offset + i];
                errorSum += diff * diff;
            }

            // Mean Squared Error invertieren
            double mse = errorSum / (pattern.Length / 2);
            return Math.Max(0, 1.0 - (mse * 100)); // Skalierung empirisch für Audio
        }

        /// <summary>
        /// Sucht den "Rising Edge Zero Crossing" (Nulldurchgang von Minus nach Plus)
        /// VOR dem lautesten Peak im Fenster. Das ist der sauberste Schnittpunkt für Audio.
        /// </summary>
        public static long FindRisingEdgeTransient(float[] samples, long start, long end)
        {
            float maxVol = -1;
            long peakPos = start;

            // 1. Lautesten Punkt (Peak) finden
            for (long i = start; i < end; i++)
            {
                float vol = Math.Abs(samples[i]);
                if (vol > maxVol)
                {
                    maxVol = vol;
                    peakPos = i;
                }
            }

            // Wenn Stille, einfach Mitte zurückgeben
            if (maxVol < 0.01f) return (start + end) / 2;

            // 2. Vom Peak rückwärts gehen und Nulldurchgang suchen
            // Wir suchen exakt den Moment, wo das Signal von Negativ auf Positiv wechselt (Rising Edge)
            // oder von Positiv auf Negativ, solange es einheitlich ist.
            // Rising Edge ( / ) ist am besten für Beats.

            long cursor = peakPos;
            while (cursor > start)
            {
                // Haben wir einen Nulldurchgang von MINUS nach PLUS? (Rising)
                if (samples[cursor] >= 0 && samples[cursor - 1] < 0)
                {
                    return cursor;
                }
                cursor--;
            }

            // Fallback: Wenn kein sauberer Rising Edge gefunden wurde, nimm einfach den Peak-Start (weniger gut)
            return peakPos;
        }

        #endregion Private Methods
    }
}