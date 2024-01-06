using System;
using System.Runtime.Serialization;

namespace SportDJ.Commons.Exceptions
{
    [Serializable]
    public class NoDeviceAvailableException
        : Exception
    {
        #region Public Constructors

        public NoDeviceAvailableException()
            : this("There is no output device available.")
        { }

        public NoDeviceAvailableException(int deviceNumber)
            : this($"There is no output device with a device number {deviceNumber} available.")
        { }

        public NoDeviceAvailableException(string message)
            : base(message)
        { }

        public NoDeviceAvailableException(string message, Exception innerException)
            : base(message: message, innerException: innerException)
        { }

        #endregion Public Constructors

        #region Protected Constructors

        protected NoDeviceAvailableException(SerializationInfo info, StreamingContext context)
            : base(info: info, context: context)
        { }

        #endregion Protected Constructors
    }
}