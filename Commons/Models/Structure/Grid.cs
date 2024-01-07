using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    public class Grid
    {
        #region Public Properties

        public List<Cell> Cells { get; set; } = new List<Cell>();

        [XmlAttribute]
        public string Name { get; set; }

        #endregion Public Properties
    }
}