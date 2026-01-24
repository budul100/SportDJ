using System.Collections.Generic;
using System.Xml.Serialization;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Interfaces;

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

        [XmlIgnore]
        public IPlayerService PlayerService { get; set; }

        [XmlAttribute]
        public bool ResetPosition { get; set; }

        [XmlIgnore]
        public override int Rows => 1;

        [XmlIgnore]
        public IStackService StackService { get; set; }

        #endregion Public Properties
    }
}