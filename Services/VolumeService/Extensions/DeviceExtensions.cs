using NAudio.CoreAudioApi;

namespace SportDJ.Services.VolumeService.Extensions
{
    internal static class DeviceExtensions
    {
        #region Public Methods

        public static bool IsMatching(this MMDevice device, string deviceName)
        {
            return device.FriendlyName.StartsWith(deviceName)
                || deviceName.StartsWith(device.FriendlyName);
        }

        #endregion Public Methods
    }
}