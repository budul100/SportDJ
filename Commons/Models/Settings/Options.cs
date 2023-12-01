using Score2Stream.Commons;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        public int FadeDurationMixIn { get; set; }

        public int FadeDurationMixOut { get; set; }

        public int FadeDurationPause { get; set; }

        public double SelectNextPortion { get; set; }

        public double TrimRearPortion { get; set; } = Constants.TrimRearPortionDefault;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}