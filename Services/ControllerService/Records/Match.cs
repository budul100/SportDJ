using System.Xml.Serialization;

namespace SportDJ.Services.ControllerService.Records;

public record Match
{
    #region Public Properties

    [XmlElement(ElementName = "ProductNameExcluded")]
    public string ProductNameExcluded { get; init; }

    [XmlElement(ElementName = "ProductNameIncluded")]
    public string ProductNameIncluded { get; init; }

    #endregion Public Properties
}