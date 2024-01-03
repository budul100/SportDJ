using System;
using System.Runtime.Serialization;

namespace SportDJ.Commons.Exceptions
{
    [Serializable]
    public class DeviceNotAvailableException
        : Exception
    {
        #region Public Constructors

        public DeviceNotAvailableException()
        { }

        public DeviceNotAvailableException(string deviceName)
            : base(GetMessage(deviceName))
        { }

        public DeviceNotAvailableException(string deviceName, Exception innerException)
            : base(message: GetMessage(deviceName), innerException: innerException)
        { }

        #endregion Public Constructors

        #region Protected Constructors

        protected DeviceNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info: info, context: context)
        { }

        #endregion Protected Constructors

        #region Private Methods

        private static string GetMessage(string deviceName)
        {
            return $"Device {deviceName} is not available.";
        }

        #endregion Private Methods
    }
}