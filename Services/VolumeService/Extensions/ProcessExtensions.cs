using System;
using System.Diagnostics;

namespace SportDJ.Services.VolumeService.Extensions
{
    internal static class ProcessExtensions
    {
        #region Public Methods

        public static bool ProcessExists(this uint processId)
        {
            try
            {
                var process = Process.GetProcessById((int)processId);
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