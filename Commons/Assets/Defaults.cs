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

        public const double TrimRearPortion = 0.1;

        public const double VolumePortionApps = 0.2;
        public const double VolumePortionBackground = 0.2;

        #endregion Public Fields
    }
}