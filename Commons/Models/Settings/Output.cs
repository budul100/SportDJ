using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Output
    {
        #region Public Properties

        public string DeviceName { get; set; }

        public int SampleRate { get; set; }

        [XmlAttribute]
        public OutputType Type { get; set; }

        public float Volume { get; set; } = Constants.VolumeMax;

        public double VolumePortionApps { get; set; } = Defaults.VolumePortionApps;

        public double VolumePortionBackground { get; set; } = Defaults.VolumePortionBackground;

        #endregion Public Properties
    }
}