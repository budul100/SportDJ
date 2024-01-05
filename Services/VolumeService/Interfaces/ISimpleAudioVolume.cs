using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleAudioVolume
    {
        #region Public Methods

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int GetMute(out bool pbMute);

        [PreserveSig]
        int SetMasterVolume(float fLevel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int SetMute(bool bMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        #endregion Public Methods
    }
}