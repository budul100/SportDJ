using Score2Stream.Commons;
using SportDJ.Commons.Enums;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Output
    {
        #region Public Properties

        public string DeviceName { get; set; }

        [XmlAttribute]
        public OutputType Type { get; set; }

        public float Volume { get; set; } = Constants.VolumeMax;

        public double VolumePortionBackground { get; set; } = Constants.VolumePortionBackgroundDefault;

        #endregion Public Properties
    }
}