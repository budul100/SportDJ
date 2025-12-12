using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Contents
{
    public class Device(string name, DeviceType type, int channels, int number = default)
    {
        #region Public Properties

        public int Channels { get; } = channels;

        public string Name { get; } = name;

        public int Number { get; } = number;

        public DeviceType Type { get; } = type;

        #endregion Public Properties
    }
}