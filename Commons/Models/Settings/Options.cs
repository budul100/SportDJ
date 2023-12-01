using Score2Stream.Commons;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        public int FadeDurationOnBackground { get; set; }

        public int FadeDurationOnMixIn { get; set; }

        public int FadeDurationOnMixOut { get; set; }

        public int FadeDurationOnPause { get; set; }


        public double SelectNextPortion { get; set; }

        public double TrimRearPortion { get; set; } = Constants.TrimRearPortionDefault;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}