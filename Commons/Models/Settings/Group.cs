using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Group
        : Cell, IGrid
    {
        #region Public Properties

        [XmlAttribute]
        public int Columns { get; set; }

        public List<Cell> Contents { get; set; } = new List<Cell>();

        [XmlAttribute]
        public int Rows { get; set; }

        #endregion Public Properties
    }
}