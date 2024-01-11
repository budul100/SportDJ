using SharpHook.Native;
using SportDJ.Commons.Assets;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

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
        public KeyCode? KeyPauseOutput { get; set; } = Defaults.KeyPauseOutput;

        [IgnoreDataMember]
        public KeyCode? KeyTalkVolume { get; set; } = Defaults.KeyCodeLowerVolume;

        [DisplayName("Select next (s)")]
        public int SelectNextSeconds { get; set; } = Defaults.SelectNextSeconds;

        [Range(0, 1)]
        [DisplayName("Trim rear")]
        public double TrimRearPortion { get; set; } = Defaults.TrimRearPortion;

        [Range(0, 1)]
        [DisplayName("Cue volume")]
        public float VolumeCue { get; set; }

        #endregion Public Properties
    }
}