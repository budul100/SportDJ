namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        public int FadeOnBackgroundMilliseconds { get; set; }

        public int FadeOnMixInMilliseconds { get; set; }

        public int FadeOnMixOutMilliseconds { get; set; }

        public int FadeOnPauseMilliseconds { get; set; }

        public double SelectNextPortion { get; set; }

        public double TrimRearPortion { get; set; } = Constants.TrimRearPortionDefault;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}