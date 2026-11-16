using System.Xml.Serialization;

namespace SportDJ.Services.ControllerService.Records;

public record Profile
{
    [XmlElement(ElementName = "Match")]
    public Match Match { get; init; }

    [XmlElement(ElementName = "InitSysEx")]
    public string InitSysEx { get; init; }

    [XmlElement(ElementName = "FinalSysEx")]
    public string FinalSysEx { get; init; }

    [XmlAttribute(AttributeName = "name")]
    public string Name { get; init; }
}