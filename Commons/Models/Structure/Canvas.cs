using SportDJ.Commons.Models.Interfaces;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Structure
{
    public class Canvas
        : IGrid
    {
        #region Public Properties

        [XmlAttribute]
        public int Columns { get; set; }

        public List<Element> Contents { get; set; } = new List<Element>();

        [XmlAttribute]
        public int Rows { get; set; }

        #endregion Public Properties
    }
}