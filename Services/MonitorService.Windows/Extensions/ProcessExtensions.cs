using System.Diagnostics;

namespace SportDJ.Services.MonitorService.Windows.Extensions
{
    internal static class ProcessExtensions
    {
        #region Public Methods

        public static bool ProcessExists(this uint processId)
        {
            var process = default(Process);

            try
            {
                process = Process.GetProcessById((int)processId);
            }
            catch
            { }

            return process != default;
        }

        #endregion Public Methods
    }
}