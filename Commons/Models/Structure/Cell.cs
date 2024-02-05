using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    [XmlInclude(typeof(Group))]
    [XmlInclude(typeof(Player))]
    public abstract class Cell
    {
        #region Public Properties

        [XmlAttribute]
        public int Column { get; set; }

        [XmlIgnore]
        public virtual int Columns { get; set; }

        [XmlAttribute]
        public int Row { get; set; }

        [XmlIgnore]
        public virtual int Rows { get; set; }

        #endregion Public Properties
    }
}