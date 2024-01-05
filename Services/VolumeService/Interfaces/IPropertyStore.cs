using SportDJ.Services.VolumeService.Structs;
using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Interfaces
{
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        #region Public Methods

        [PreserveSig]
        int Commit();

        [PreserveSig]
        int GetAt(int iProp, out PROPERTYKEY pkey);

        [PreserveSig]
        int GetCount(out int cProps);

        [PreserveSig]
        int GetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);

        [PreserveSig]
        int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);

        #endregion Public Methods
    }
}