namespace SportDJ.Commons
{
    public static class Constants
    {
        #region Public Fields

        public const string AppName = nameof(SportDJ);

        public const string FileNameSession = "Session.xml";
        public const string FileNameSettings = "Settings.xml";

        public const int MixerSourcesMax = 1024;

        public const string PlayerNamePFL = "PFL";

        public const int SampleRateDefault = 44100;
        public const double TrimRearPortionDefault = 0.1;

        public const float VolumeMax = 1f;
        public const float VolumeMin = 0f;

        public const double VolumePortionBackgroundDefault = 0.5;

        #endregion Public Fields
    }
}