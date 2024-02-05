using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    public class Grid
    {
        #region Public Properties

        public List<Cell> Cells { get; set; } = new List<Cell>();

        [XmlIgnore]
        public int Columns { get; set; }

        [XmlAttribute]
        public string KeyCode { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlIgnore]
        public int Rows { get; set; }

        #endregion Public Properties
    }
}