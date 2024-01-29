using System.Drawing;

namespace SportDJ.Commons.Assets
{
    public static class Constants
    {
        #region Public Fields

        public const string AppName = nameof(SportDJ);

        // Duration of a volume change step. Should not be greater than 100.
        public const int FadeStepInMilliSec = 30;

        public const string FormatClock = "HH:mm:ss";
        public const string FormatDuration = @"mm\:ss\.f";

        public const int IndexAudioDeviceMin = 0;
        public const int IndexAudioSessionMin = 0;

        public const int MenuGroupAdd = 10;
        public const int MenuItemsSize = 20;
        public const int MixerSourcesMax = 1024;

        public const double PercentageIncrement = 1;
        public const double PercentageMax = 100;
        public const double PercentageMin = 0;

        public const int SecondsIsEnding = 10;

        public const string SplitterTitle = " | ";
        public const char SplitterVersion = '.';

        public const float VolumeMax = 1f;
        public const float VolumeMin = 0f;

        public const int WidthBase = 1000;

        public static Color ColorPeakBottom = Color.DarkGreen;
        public static Color ColorPeakTop = Color.Green;

        #endregion Public Fields
    }
}