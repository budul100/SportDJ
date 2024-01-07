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

        public KeyCode? KeyLowerVolume { get; set; } = Defaults.KeyCodeLowerVolume;

        public KeyCode? KeyPauseOutput { get; set; } = Defaults.KeyPauseOutput;

        public int SelectNextSeconds { get; set; } = Defaults.SelectNextSeconds;

        public double TrimRearPortion { get; set; } = Defaults.TrimRearPortion;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}