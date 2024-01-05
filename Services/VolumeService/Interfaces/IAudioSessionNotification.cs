using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionNotification
    {
        #region Public Methods

        void OnSessionCreated(IAudioSessionControl NewSession);

        #endregion Public Methods
    }
}