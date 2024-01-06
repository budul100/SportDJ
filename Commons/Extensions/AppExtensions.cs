using System;

namespace SportDJ.Commons.Extensions
{
    public static class AppExtensions
    {
        #region Public Methods

        public static string GetProcessName()
        {
            var name = AppDomain.CurrentDomain.FriendlyName;

            var result = name?.Contains(".exe") == true
                ? name[..^".exe".Length]
                : name;

            return result;
        }

        #endregion Public Methods
    }
}