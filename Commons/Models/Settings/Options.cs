using SharpHook.Native;

namespace SportDJ.Commons.Models.Settings
{
    public class Options
    {
        #region Public Properties

        public int FadeOnLowerMilliseconds { get; set; }

        public int FadeOnMixInMilliseconds { get; set; }

        public int FadeOnMixOutMilliseconds { get; set; }

        public int FadeOnPauseMilliseconds { get; set; }

        public KeyCode? KeyPauseOutput { get; set; } = Defaults.KeyPauseOutput;

        public KeyCode? KeyTalkVolume { get; set; } = Defaults.KeyCodeLowerVolume;

        public int SelectNextSeconds { get; set; } = Defaults.SelectNextSeconds;

        public double TrimRearPortion { get; set; } = Defaults.TrimRearPortion;

        public float VolumeCue { get; set; }

        #endregion Public Properties
    }
}