using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        #region Public Methods

        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl Session);

        #endregion Public Methods
    }
}