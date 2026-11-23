using System.Xml.Serialization;
using SportDJ.Commons.Enums;

namespace SportDJ.Services.ControllerService.Records;

public record ProfileColor
{
    #region Public Properties

    [XmlAttribute(AttributeName = "name")]
    public PlayerColorType Name { get; init; }

    /// <summary>
    /// Value which is sent as velocity (Note) or value (Control) to light the key in this color.
    /// </summary>
    [XmlAttribute(AttributeName = "value")]
    public int Value { get; init; }

    #endregion Public Properties
}
