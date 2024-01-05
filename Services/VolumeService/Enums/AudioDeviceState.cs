namespace SportDJ.Services.VolumeService.Enums
{
    public enum AudioDeviceState
    {
        Active = 0x1,

        Disabled = 0x2,

        NotPresent = 0x4,

        Unplugged = 0x8,
    }
}