namespace LoopDetector
{
    public struct LoopPoints
    {
        #region Public Fields

        public double Confidence;
        public long EndSample;
        public long StartSample;

        #endregion Public Fields

        #region Public Properties

        // 0.0 bis 1.0 (wie gut passen Start und Ende zusammen?)
        public readonly bool IsValid => StartSample < EndSample && Confidence > 0.85;

        #endregion Public Properties
    }
}