using SportDJ.Commons.Models.Interfaces;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    public class Player
        : Cell, INameable, IKeyable
    {
        #region Public Properties

        [XmlIgnore]
        public override int Columns => 1;

        [XmlAttribute]
        public bool IsForeground { get; set; }

        [XmlAttribute]
        public bool IsLoop { get; set; }

        [XmlAttribute]
        public string KeyCode { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlArrayItem(ElementName = "Path")]
        public List<string> Paths { get; set; } = [];

        [XmlAttribute]
        public bool ResetPosition { get; set; }

        [XmlIgnore]
        public override int Rows => 1;

        #endregion Public Properties
    }
}