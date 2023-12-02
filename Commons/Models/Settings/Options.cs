namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        public int FadeOnBackgroundMilliseconds { get; set; }

        public int FadeOnMixInMilliseconds { get; set; }

        public int FadeOnMixOutMilliseconds { get; set; }

        public int FadeOnPauseMilliseconds { get; set; }

        public int SelectNextSeconds { get; set; } = 10;

        public double TrimRearPortion { get; set; } = Defaults.TrimRearPortion;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}