using System;
using System.Runtime.Serialization;

namespace SportDJ.Commons.Exceptions
{
    [Serializable]
    public class DeviceNotDefinedException
        : Exception
    {
        #region Public Constructors

        public DeviceNotDefinedException()
            : this("There is no output device defined.")
        { }

        public DeviceNotDefinedException(string message)
            : base(message)
        { }

        public DeviceNotDefinedException(string message, Exception innerException)
            : base(message: message, innerException: innerException)
        { }

        #endregion Public Constructors

        #region Protected Constructors

        protected DeviceNotDefinedException(SerializationInfo info, StreamingContext context)
            : base(info: info, context: context)
        { }

        #endregion Protected Constructors
    }
}