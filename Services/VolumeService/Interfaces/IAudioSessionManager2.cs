using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        #region Public Methods

        [PreserveSig]
        int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        [PreserveSig]
        int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);

        int RegisterDuckNotificationNotImpl();

        [PreserveSig]
        int RegisterSessionNotification(IAudioSessionNotification SessionNotification);

        int UnregisterDuckNotificationNotImpl();

        [PreserveSig]
        int UnregisterSessionNotification(IAudioSessionNotification SessionNotification);

        #endregion Public Methods
    }
}