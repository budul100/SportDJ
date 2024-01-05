using System;

namespace SportDJ.Services.VolumeService.Enums
{
    [Flags]
    internal enum CLSCTX
    {
        CLSCTX_INPROC_SERVER = 0x1,

        CLSCTX_INPROC_HANDLER = 0x2,

        CLSCTX_LOCAL_SERVER = 0x4,

        CLSCTX_REMOTE_SERVER = 0x10,

        CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
    }
}