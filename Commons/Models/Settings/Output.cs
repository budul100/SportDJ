using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Output
    {
        #region Public Properties

        public string DeviceName { get; set; }

        [XmlIgnore]
        public string DeviceNameCurrent { get; set; }

        public int SampleRate { get; set; }

        [XmlAttribute]
        public OutputType Type { get; set; }

        public float Volume { get; set; } = Constants.VolumeMax;

        public int VolumePercentageBackground { get; set; } = Defaults.VolumePercentageBackground;

        public int VolumePercentageExternals { get; set; } = Defaults.VolumePercentageExternals;

        #endregion Public Properties
    }
}