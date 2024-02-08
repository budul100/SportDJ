using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        [DisplayName("Disable interrupting dialogs")]
        public bool DisableInterruptingDialogs { get; set; }

        [Category("Fade durations")]
        [DisplayName("Fade lower (ms)")]
        public int FadeOnLowerMilliseconds { get; set; } = Defaults.FadeOnLowerMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade mix-in (ms)")]
        public int FadeOnMixInMilliseconds { get; set; } = Defaults.FadeOnMixInMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade mix-out (ms)")]
        public int FadeOnMixOutMilliseconds { get; set; } = Defaults.FadeOnMixOutMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade pause (ms)")]
        public int FadeOnPauseMilliseconds { get; set; } = Defaults.FadeOnPauseMilliseconds;

        [IgnoreDataMember]
        public string KeyCodeOutputPause { get; set; } = Defaults.KeyCodeOutputPause;

        [IgnoreDataMember]
        public string KeyCodeScreenChange { get; set; } = Defaults.KeyCodeScreenChange;

        [IgnoreDataMember]
        public string KeyCodeVolumeTalk { get; set; } = Defaults.KeyCodeVolumeTalk;

        [DisplayName("Select next on pause (s)")]
        public int? SelectNextOnPauseSeconds { get; set; } = Defaults.SelectNextOnPauseSeconds;

        [DisplayName("Select next on play (s)")]
        public int? SelectNextOnPlaySeconds { get; set; } = Defaults.SelectNextOnPlaySeconds;

        [Range(Constants.PercentageMin, Constants.PercentageMax)]
        [DisplayName("Trim rear (%)")]
        public int TrimRearPercentage { get; set; } = Defaults.TrimRearPercentage;

        [Range(Constants.PercentageMin, Constants.PercentageMax)]
        [DisplayName("Cue volume for trim (%)")]
        public int TrimVolumePercentage { get; set; } = Defaults.TrimVolumePercentage;

        #endregion Public Properties
    }
}