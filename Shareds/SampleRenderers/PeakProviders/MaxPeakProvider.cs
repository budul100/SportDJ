using System.Linq;

namespace NAudio.WaveFormRenderer.PeakProviders;

public class MaxPeakProvider
    : PeakProvider
{
    #region Public Methods

    public override PeakInfo GetNextPeak()
    {
        var samplesRead = Provider.Read(ReadBuffer);
        var max = samplesRead == 0 ? 0 : ReadBuffer.Take(samplesRead).Max();
        var min = samplesRead == 0 ? 0 : ReadBuffer.Take(samplesRead).Min();
        return new PeakInfo(min, max);
    }

    #endregion Public Methods
}
