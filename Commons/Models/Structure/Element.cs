using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    [XmlInclude(typeof(Group))]
    [XmlInclude(typeof(Player))]
    public abstract class Element
    {
        #region Public Properties

        [XmlAttribute]
        public int Column { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Row { get; set; }

        #endregion Public Properties
    }
}