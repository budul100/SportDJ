using Score2Stream.Commons;
using SportDJ.Commons.Models.Interfaces;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    public class Set
        : IGrid
    {
        #region Public Properties

        [XmlAttribute]
        public int Columns { get; set; }

        public List<Cell> Contents { get; set; } = new List<Cell>();

        public int FadeInDurationMix { get; set; }

        [XmlAttribute]
        public int Rows { get; set; }

        public double SelectNextPortion { get; set; }

        public double TrimRearPortion { get; set; } = Constants.TrimRearPortionDefault;

        public float TrimVolume { get; set; }

        #endregion Public Properties
    }
}