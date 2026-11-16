using System;

namespace SportDJ.Services.DeviceService.Windows.Exceptions;

[Serializable]
public class NoDeviceAvailableException
    : Exception
{
    #region Public Constructors

    public NoDeviceAvailableException()
    { }

    public NoDeviceAvailableException(string message)
        : base(message)
    { }

    public NoDeviceAvailableException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    { }

    #endregion Public Constructors
}