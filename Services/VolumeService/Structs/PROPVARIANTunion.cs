using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct PROPVARIANTunion
    {
        [FieldOffset(0)]
        public int lVal;

        [FieldOffset(0)]
        public ulong uhVal;

        [FieldOffset(0)]
        public short boolVal;

        [FieldOffset(0)]
        public IntPtr pwszVal;

        [FieldOffset(0)]
        public IntPtr puuid;
    }
}