using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Contents
{
    [XmlInclude(typeof(Group))]
    [XmlInclude(typeof(Controller))]
    public abstract class Cell
    {
        #region Public Properties

        [XmlAttribute]
        public int Column { get; set; }

        public string Name { get; set; }

        [XmlAttribute]
        public int Row { get; set; }

        #endregion Public Properties
    }
}