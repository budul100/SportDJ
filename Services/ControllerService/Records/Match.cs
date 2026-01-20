using System.Xml.Serialization;

namespace SportDJ.Services.ControllerService.Records
{
    public record Match
    {
        [XmlElement(ElementName = "ProductNameIncluded")]
        public string ProductNameIncluded { get; init; }

        [XmlElement(ElementName = "ProductNameExcluded")]
        public string ProductNameExcluded { get; init; }
    }
}