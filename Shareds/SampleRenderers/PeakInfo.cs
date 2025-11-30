namespace NAudio.WaveFormRenderer
{
    public class PeakInfo
    {
        #region Public Constructors

        public PeakInfo(float min, float max)
        {
            Max = max;
            Min = min;
        }

        #endregion Public Constructors

        #region Public Properties

        public float Max { get; private set; }

        public float Min { get; private set; }

        #endregion Public Properties
    }
}