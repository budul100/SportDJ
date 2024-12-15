using SportDJ.Commons.Assets;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        [DisplayName("Enable interrupting dialogs")]
        public bool EnableInterruptingDialogs { get; set; }

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
        [DisplayName("Fade pause w/ effect (ms)")]
        public int FadeOnPauseFilteredMilliseconds { get; set; } = Defaults.FadeOnPauseFilteredMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade pause w/o effect (ms)")]
        public int FadeOnPauseUnfilteredMilliseconds { get; set; } = Defaults.FadeOnPauseUnfilteredMilliseconds;

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