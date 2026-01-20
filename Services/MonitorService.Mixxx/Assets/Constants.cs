namespace SportDJ.Services.MonitorService.Mixxx.Assets
{
    public static class Constants
    {
        #region Public Fields

        public const int MidiChannel = 1;
        public const int MidiMixxxDeckCount = 4;

        public const string MidiMixxxMappingFile = "SportDJ2Mixxx.midi.xml";
        public const string MidiMixxxMappingSource = "Midi";
        public const string MidiMixxxMappingTarget = "Mixxx\\controllers";
        public const string MidiMixxxOutputDevice = "SportDJ2Mixxx";

        public const int MidiNoteBase = 0x40;
        public const int MidiNoteVolume = 0x07;
        public const int MidiVolumeMax = 127;
        public const int MidiVolumeMin = 0;

        #endregion Public Fields
    }
}