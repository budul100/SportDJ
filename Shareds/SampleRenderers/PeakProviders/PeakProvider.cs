using NAudio.Wave;

namespace NAudio.WaveFormRenderer.PeakProviders;

public abstract class PeakProvider 
    : IPeakProvider
{
    #region Protected Properties

    protected ISampleProvider Provider { get; private set; }

    protected float[] ReadBuffer { get; private set; }

    protected int SamplesPerPeak { get; private set; }

    #endregion Protected Properties

    #region Public Methods

    public abstract PeakInfo GetNextPeak();

    public void Init(ISampleProvider provider, int samplesPerPeak)
    {
        Provider = provider;
        SamplesPerPeak = samplesPerPeak;
        ReadBuffer = new float[samplesPerPeak];
    }

    #endregion Public Methods
}