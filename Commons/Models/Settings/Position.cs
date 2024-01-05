using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Position
    {
        #region Public Properties

        public int Index { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        public string Path { get; set; }

        #endregion Public Properties
    }
}