using System.Collections.Generic;
using System.Xml.Serialization;
using SportDJ.Commons.Models.Interfaces;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Models.Structure;

public class Grid
    : INameable, IKeyable
{
    #region Public Properties

    [XmlArray(Order = 1)]
    public List<Cell> Cells { get; set; } = [];

    [XmlIgnore]
    public int Columns { get; set; }

    [XmlIgnore]
    public Group Group { get; set; }

    /// <summary>
    /// The MIDI key, which selects the tab.
    /// </summary>
    [XmlElement(Order = 0)]
    public Key Key { get; set; }

    [XmlAttribute]
    public string KeyCode { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlIgnore]
    public int Rows { get; set; }

    #endregion Public Properties

    #region Public Methods

    // Tabs without a MIDI key do not write an empty key
    public bool ShouldSerializeKey() => Key != default;

    #endregion Public Methods
}
