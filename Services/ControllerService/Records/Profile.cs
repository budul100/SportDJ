using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Services.ControllerService.Records;

public record Profile
{
    #region Public Properties

    [XmlArray(ElementName = "Colors")]
    [XmlArrayItem(ElementName = "Color")]
    public List<ProfileColor> Colors { get; init; } = [];

    [XmlElement(ElementName = "FinalSysEx")]
    public string FinalSysEx { get; init; }

    [XmlElement(ElementName = "InitSysEx")]
    public string InitSysEx { get; init; }

    [XmlElement(ElementName = "Match")]
    public Match Match { get; init; }

    [XmlAttribute(AttributeName = "name")]
    public string Name { get; init; }

    #endregion Public Properties
}
