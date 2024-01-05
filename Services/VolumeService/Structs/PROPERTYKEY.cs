using System;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY
    {
        public Guid fmtid;

        public int pid;

        public override string ToString()
        {
            return fmtid.ToString("B") + " " + pid;
        }
    }
}