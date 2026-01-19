using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Records
{
    public record Device(string Name, DeviceType Type, int Channels, int Number = default);
}