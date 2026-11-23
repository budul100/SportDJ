using SkiaSharp;

namespace SportDJ.Commons.Assets;

public static class Constants
{
    #region Public Fields

    // Duration of a volume change step. Should not be greater than 100.
    public const int FadeStepInMilliSec = 30;

    public const int FilterCutoffFrequency = 20000;
    public const int FilterCutoffMax = 150;

    public const string FormatClock = "HH:mm:ss";
    public const string FormatDuration = @"mm\:ss\.f";

    public const int IndexAudioDeviceMin = 0;
    public const int IndexAudioSessionMin = 0;

    public const int IsEndingSeconds = 10;

    public const int MidiDelayStopDecks = 100;

    public const int MixerSourcesMax = 1024;

    public const int NameMaxLength = 30;

    public const int OptionsFadeInc = 100;
    public const int OptionsFadeMax = 10000;
    public const int OptionsFadeMin = 0;

    public const double OptionsPercentageInc = 5;
    public const double OptionsPercentageMax = 100;
    public const double OptionsPercentageMin = 0;

    public const int OptionsSelectionInc = 100;
    public const int OptionsSelectionMax = 10000;
    public const int OptionsSelectionMin = 0;

    public const int OutputChannelMax = 2;

    public const int SampleRateDefaultAsio = 48000;

    public const string SettingsExtension = ".xml";

    public const string SplitterTitle = " | ";
    public const char SplitterVersion = '.';

    // Update frequency of audio signals in ms
    public const long UpdatesInMilliSec = 50;

    public const float VolumeMax = 1f;
    public const float VolumeMin = 0f;

    public const long WidthBase = 1000;

    public static readonly SKColor ColorPeakBottom = SKColors.DarkGreen;
    public static readonly SKColor ColorPeakTop = SKColors.Green;

    #endregion Public Fields
}
