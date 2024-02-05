using System.Drawing;

namespace SportDJ.Commons.Assets
{
    public static class Constants
    {
        #region Public Fields

        public const string AppName = nameof(SportDJ);

        public const string CloseApplicationHeader = "Close application";
        public const string CloseApplicationMessage = "Shall the application be closed?";

        // Duration of a volume change step. Should not be greater than 100.
        public const int FadeStepInMilliSec = 30;

        public const string FormatClock = "HH:mm:ss";
        public const string FormatDuration = @"mm\:ss\.f";

        public const string GridAddPlayerHeader = "Add player on";
        public const string GridAddTabHeader = "Add tab";
        public const string GridEditHeader = "Edit grid";
        public const string GridMoveHeader = "Move to";
        public const string GridRemoveHeader = "Remove element";
        public const string GridRemoveMessage = "Shall the element be removed?";
        public const string GridSwapGroupMessage = "Shall the group swapped into a player?";
        public const string GridSwapHeader = "Swap element";
        public const string GridSwapPlayerMessage = "Shall the player swapped into a group?";

        public const int IndexAudioDeviceMin = 0;
        public const int IndexAudioSessionMin = 0;

        public const int IsEndingSeconds = 10;

        public const int MenuGroupBaseSize = 20;
        public const int MenuGroupTextLength = 10;

        public const int MixerSourcesMax = 1024;

        public const double PercentageIncrement = 1;
        public const double PercentageMax = 100;
        public const double PercentageMin = 0;

        public const string PlayerClearHeader = "Clear Player";
        public const string PlayerClearMessage = "Shall the player be cleared?";
        public const string PlayerOnForegroundHeader = "On foreground";

        public const string SelectionFileHeader = "Select File";
        public const string SelectionFolderHeader = "Select Folder";

        public const string SplitterTitle = " | ";
        public const char SplitterVersion = '.';

        public const float VolumeMax = 1f;
        public const float VolumeMin = 0f;

        public const long WidthBase = 1000;

        public static Color ColorPeakBottom = Color.DarkGreen;
        public static Color ColorPeakTop = Color.Green;

        #endregion Public Fields
    }
}