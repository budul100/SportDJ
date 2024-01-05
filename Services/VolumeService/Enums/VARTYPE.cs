using System;

namespace SportDJ.Services.VolumeService.Enums
{
    [Flags]
    internal enum VARTYPE
             : short
    {
        VT_I4 = 3,

        VT_BOOL = 11,

        VT_UI4 = 19,

        VT_LPWSTR = 31,

        VT_BLOB = 65,

        VT_CLSID = 72,
    }
}