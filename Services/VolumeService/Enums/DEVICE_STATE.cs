namespace SportDJ.Services.VolumeService.Enums
{
    internal enum DEVICE_STATE
    {
        ACTIVE = 0x00000001,

        DISABLED = 0x00000002,

        NOTPRESENT = 0x00000004,

        UNPLUGGED = 0x00000008,

        MASK_ALL = 0x0000000F
    }
}