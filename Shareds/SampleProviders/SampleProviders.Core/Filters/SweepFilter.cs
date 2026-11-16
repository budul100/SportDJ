using System;

namespace SportDJ.Shareds.SampleProviders.Core.Filters;

/// <summary>
/// DJ style sweep filter: resonant low pass (TPT state variable filter) whose cutoff
/// sweeps exponentially downwards with an optional wah wobble. Driven by a fade progress
/// value (0 to 1). Coefficients are only recalculated every <see cref="ControlBlockFrames"/> frames.
/// </summary>
public class SweepFilter(int sampleRate, int channels, double startCutoffFrequency)
{
    #region Private Fields

    private const double ButterworthQ = 0.7071067811865476;
    private const int ControlBlockFrames = 32;
    private const double MinCutoffFrequency = 20.0;
    private const float SoftLimitKnee = 0.9f;

    private readonly double[] ic1eq = new double[channels];
    private readonly double[] ic2eq = new double[channels];

    private double a1;
    private double a2;
    private double a3;
    private int blockCounter;
    private double lfoPhase;

    #endregion Private Fields

    #region Public Properties

    /// <summary>Cutoff at the end of the sweep in Hz.</summary>
    public double EndCutoffFrequency { get; set; } = 200.0;

    /// <summary>Peak resonance reached during the sweep (0.7 = none, 4 to 8 = pronounced).</summary>
    public double ResonanceQ { get; set; } = 4.0;

    /// <summary>Wobble amount in octaves of cutoff modulation (strongest mid sweep, 0 disables).</summary>
    public double WobbleDepthOctaves { get; set; } = 0.3;

    /// <summary>Speed of the wah wobble in Hz.</summary>
    public double WobbleRateHz { get; set; } = 3.0;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Call once per frame (before <see cref="Transform"/> of the channels).
    /// Progress is the fade progress from 0 (start) to 1 (end).
    /// </summary>
    public void BeginFrame(double progress)
    {
        if (blockCounter == 0)
        {
            UpdateCoefficients(Math.Clamp(progress, 0.0, 1.0));
        }

        blockCounter = (blockCounter + 1) % ControlBlockFrames;
    }

    public float Transform(int channel, float input)
    {
        var v3 = input - ic2eq[channel];
        var v1 = a1 * ic1eq[channel] + a2 * v3;
        var v2 = ic2eq[channel] + a2 * ic1eq[channel] + a3 * v3;

        ic1eq[channel] = 2.0 * v1 - ic1eq[channel];
        ic2eq[channel] = 2.0 * v2 - ic2eq[channel];

        return SoftLimit((float)v2);
    }

    #endregion Public Methods

    #region Private Methods

    private static float SoftLimit(float value)
    {
        // Resonance can push peaks over full scale, only values above the knee are touched
        var abs = Math.Abs(value);

        if (abs <= SoftLimitKnee)
        {
            return value;
        }

        var limited = SoftLimitKnee
            + (1.0f - SoftLimitKnee) * MathF.Tanh((abs - SoftLimitKnee) / (1.0f - SoftLimitKnee));

        return MathF.CopySign(limited, value);
    }

    private void UpdateCoefficients(double progress)
    {
        // Exponential sweep: constant number of octaves per second
        var cutoff = startCutoffFrequency * Math.Pow(EndCutoffFrequency / startCutoffFrequency, progress);

        // Wah wobble, zero at start and end of the sweep
        var wobbleEnvelope = Math.Sin(Math.PI * progress);
        cutoff *= Math.Pow(2.0, WobbleDepthOctaves * wobbleEnvelope * Math.Sin(lfoPhase));

        lfoPhase += 2.0 * Math.PI * WobbleRateHz * ControlBlockFrames / sampleRate;

        if (lfoPhase > 2.0 * Math.PI)
        {
            lfoPhase -= 2.0 * Math.PI;
        }

        cutoff = Math.Clamp(cutoff, MinCutoffFrequency, sampleRate * 0.45);

        // Resonance builds up quickly at the beginning, then stays
        var q = ButterworthQ + (ResonanceQ - ButterworthQ) * Math.Min(1.0, progress * 4.0);
        var k = 1.0 / q;

        var g = Math.Tan(Math.PI * cutoff / sampleRate);

        a1 = 1.0 / (1.0 + g * (g + k));
        a2 = g * a1;
        a3 = g * a2;
    }

    #endregion Private Methods
}