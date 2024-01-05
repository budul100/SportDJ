using SportDJ.Services.VolumeService.Enums;
using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT
    {
        public VARTYPE vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public PROPVARIANTunion union;

        public object GetValue()
        {
            switch (vt)
            {
                case VARTYPE.VT_BOOL:
                    return union.boolVal != 0;

                case VARTYPE.VT_LPWSTR:
                    return Marshal.PtrToStringUni(union.pwszVal);

                case VARTYPE.VT_UI4:
                    return union.lVal;

                case VARTYPE.VT_CLSID:
                    return (Guid)Marshal.PtrToStructure(union.puuid, typeof(Guid));

                default:
                    return vt.ToString() + ":?";
            }
        }
    }
}