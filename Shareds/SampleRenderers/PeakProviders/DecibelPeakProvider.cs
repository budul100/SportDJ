using System;
using NAudio.Wave;

namespace NAudio.WaveFormRenderer.PeakProviders;

internal sealed class DecibelPeakProvider(IPeakProvider sourceProvider, double dynamicRange)
    : IPeakProvider
{
    #region Private Fields

    private readonly double dynamicRange = dynamicRange;
    private readonly IPeakProvider sourceProvider = sourceProvider;

    #endregion Private Fields

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