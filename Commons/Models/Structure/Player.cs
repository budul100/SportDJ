using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    public class Player
        : Cell
    {
        #region Public Properties

        [XmlAttribute]
        public string KeyCode { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        public bool OnForeground { get; set; }

        public string Path { get; set; }

        public bool SavePositions { get; set; }

        #endregion Public Properties
    }
}