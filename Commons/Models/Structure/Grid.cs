using System.Collections.Generic;
using System.Xml.Serialization;
using SportDJ.Commons.Models.Interfaces;

namespace SportDJ.Commons.Models.Structure;

public class Grid
    : INameable, IKeyable
{
    #region Public Properties

    public List<Cell> Cells { get; set; } = [];

    [XmlIgnore]
    public int Columns { get; set; }

    [XmlIgnore]
    public Group Group { get; set; }

    [XmlAttribute]
    public string KeyCode { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlIgnore]
    public int Rows { get; set; }

    #endregion Public Properties
}