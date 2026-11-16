using NAudio.Wave;

namespace NAudio.WaveFormRenderer;

public interface IPeakProvider
{
    #region Public Methods

    PeakInfo GetNextPeak();

    void Init(ISampleProvider reader, int samplesPerPixel);

    #endregion Public Methods
}