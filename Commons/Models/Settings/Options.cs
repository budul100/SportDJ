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

        [DisplayName("Enable interrupting dialogs")]
        public bool EnableInterruptingDialogs { get; set; }

        [Category("Fade durations")]
        [DisplayName("Fade lower (ms)")]
        [Range(Constants.OptionsFadeMin, Constants.OptionsFadeMax)]
        public int FadeOnLowerMilliseconds { get; set; } = Defaults.FadeOnLowerMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade mix-in (ms)")]
        [Range(Constants.OptionsFadeMin, Constants.OptionsFadeMax)]
        public int FadeOnMixInMilliseconds { get; set; } = Defaults.FadeOnMixInMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade mix-out (ms)")]
        [Range(Constants.OptionsFadeMin, Constants.OptionsFadeMax)]
        public int FadeOnMixOutMilliseconds { get; set; } = Defaults.FadeOnMixOutMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade pause w/ effect (ms)")]
        [Range(Constants.OptionsFadeMin, Constants.OptionsFadeMax)]
        public int FadeOnPauseFilteredMilliseconds { get; set; } = Defaults.FadeOnPauseFilteredMilliseconds;

        [Category("Fade durations")]
        [DisplayName("Fade pause w/o effect (ms)")]
        [Range(Constants.OptionsFadeMin, Constants.OptionsFadeMax)]
        public int FadeOnPauseUnfilteredMilliseconds { get; set; } = Defaults.FadeOnPauseUnfilteredMilliseconds;

        [IgnoreDataMember]
        public string KeyCodeOutputPause { get; set; } = Defaults.KeyCodeOutputPause;

        [IgnoreDataMember]
        public string KeyCodeScreenChange { get; set; } = Defaults.KeyCodeScreenChange;

        [IgnoreDataMember]
        public string KeyCodeVolumeTalk { get; set; } = Defaults.KeyCodeVolumeTalk;

        [DisplayName("Select next on pause (s)")]
        [Range(Constants.OptionsSelectionMin, Constants.OptionsSelectionMax)]
        public int? SelectNextOnPauseSeconds { get; set; } = Defaults.SelectNextOnPauseSeconds;

        [DisplayName("Select next on play (s)")]
        public int? SelectNextOnPlaySeconds { get; set; } = Defaults.SelectNextOnPlaySeconds;

        [DisplayName("Trim rear (%)")]
        [Range(Constants.OptionsPercentageMin, Constants.OptionsPercentageMax)]
        public int TrimRearPercentage { get; set; } = Defaults.TrimRearPercentage;

        [DisplayName("Cue volume for trim (%)")]
        [Range(Constants.OptionsPercentageMin, Constants.OptionsPercentageMax)]
        public int TrimVolumePercentage { get; set; } = Defaults.TrimVolumePercentage;

        #endregion Public Properties
    }
}