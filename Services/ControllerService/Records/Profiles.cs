using System.Collections.Generic;
using System.Xml.Serialization;

namespace SportDJ.Services.ControllerService.Records
{
    [XmlRoot(ElementName = "ControllerProfiles")]
    public record ControllerProfiles
    {
        [XmlElement(ElementName = "Profile")]
        public List<Profile> Profiles { get; init; } = [];
    }
}