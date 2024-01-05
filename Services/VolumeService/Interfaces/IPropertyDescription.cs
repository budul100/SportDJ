using SportDJ.Services.VolumeService.Structs;
using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("6f79d558-3e96-4549-a1d1-7d75d2288814"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyDescription
    {
        #region Public Methods

        [PreserveSig]
        int GetCanonicalName(out IntPtr ppszName);

        [PreserveSig]
        int GetDisplayName(out IntPtr ppszName);

        [PreserveSig]
        int GetPropertyKey(out PROPERTYKEY pkey);

        [PreserveSig]
        int GetPropertyType(out short pvartype);

        #endregion Public Methods

        // WARNING: the rest is undefined. you *can't* implement it, only use it.
    }
}