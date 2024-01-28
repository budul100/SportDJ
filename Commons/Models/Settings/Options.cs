using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using PropertyModels.ComponentModel;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        [Category("Fade durations")]
        [DisplayName("Fade lower (ms)")]
        public int FadeOnLowerMilliseconds { get; set; }

        [Category("Fade durations")]
        [DisplayName("Fade mix-in (ms)")]
        public int FadeOnMixInMilliseconds { get; set; }

        [Category("Fade durations")]
        [DisplayName("Fade mix-out (ms)")]
        public int FadeOnMixOutMilliseconds { get; set; }

        [Category("Fade durations")]
        [DisplayName("Fade pause (ms)")]
        public int FadeOnPauseMilliseconds { get; set; }

        [IgnoreDataMember]
        public string KeyCodeOutputPause { get; set; } = Defaults.KeyCodeOutputPause;

        [IgnoreDataMember]
        public string KeyCodeScreenChange { get; set; } = Defaults.KeyCodeScreenChange;

        [IgnoreDataMember]
        public string KeyCodeVolumeTalk { get; set; } = Defaults.KeyCodeVolumeTalk;

        [DisplayName("Select next (s)")]
        public int SelectNextSeconds { get; set; } = Defaults.SelectNextSeconds;

        [Range(0, 100)]
        [Trackable(min: 0, max: Constants.PercentageMax, Increment = Constants.PercentageIncrement)]
        [DisplayName("Trim rear (%)")]
        public int TrimRearPercentage { get; set; } = Defaults.TrimRearPercentage;

        [Range(0, 100)]
        [Trackable(min: 0, max: Constants.PercentageMax, Increment = Constants.PercentageIncrement)]
        [DisplayName("Cue volume (%)")]
        public int TrimVolumePercentage { get; set; } = Defaults.TrimVolumePercentage;

        #endregion Public Properties
    }
}