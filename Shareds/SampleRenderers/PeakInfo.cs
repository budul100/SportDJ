namespace NAudio.WaveFormRenderer;

public class PeakInfo(float min, float max)
{
    #region Public Properties

    public float Max { get; private set; } = max;

    public float Min { get; private set; } = min;

    #endregion Public Properties
}