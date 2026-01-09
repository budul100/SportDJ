using System;
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

        #region Public Methods

        public static bool operator !=(Device left, Device right)
        {
            return !(left == right);
        }

        public static bool operator ==(Device left, Device right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is Device device
                && Name == device.Name
                && Type == device.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                Type);
        }

        #endregion Public Methods
    }
}