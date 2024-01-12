using SharpHook.Native;

namespace SportDJ.Commons.Assets
{
    public static class Defaults
    {
        #region Public Fields

        public const int AppHeightDefault = 800;
        public const int AppWidthDefault = 1200;

        public const string FileNameSession = "Session.xml";
        public const string FileNameSettings = "Settings.xml";

        public const KeyCode KeyCodeLowerVolume = KeyCode.VcSpace;
        public const KeyCode KeyPauseOutput = KeyCode.VcEnter;

        public const int SampleRate = 44100;

        public const int SelectNextSeconds = 10;

        public const int SettingsWidth = 200;

        public const int TrimRearPercentage = 10;
        public const int TrimVolumePercentage = 0;

        public const int VolumePercentageApps = 20;
        public const int VolumePercentageBackground = 20;

        #endregion Public Fields
    }
}