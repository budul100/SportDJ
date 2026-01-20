using System;

namespace SportDJ.Services.ControllerService.Exceptions
{
    [Serializable]
    public class NoControllerAvailableException
        : Exception
    {
        #region Public Constructors

        public NoControllerAvailableException()
        { }

        public NoControllerAvailableException(string message)
            : base(message)
        { }

        public NoControllerAvailableException(string message, Exception innerException)
            : base(message: message, innerException: innerException)
        { }

        #endregion Public Constructors
    }
}