using System;

namespace SportDJ.Services.VolumeService.Windows.Extensions
{
    internal static class ProcessExtensions
    {
        #region Public Methods

        public static bool ProcessExists(this uint processId)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        #endregion Public Methods
    }
}