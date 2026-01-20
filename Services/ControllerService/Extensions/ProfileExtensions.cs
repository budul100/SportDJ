using System;
using System.Linq;

namespace SportDJ.Services.ControllerService.Extensions
{
    internal static class ProfileExtensions
    {
        #region Public Methods

        public static byte[] Parse(this string hex) => hex.Split(' ')
            .Select(b => Convert.ToByte(b, 16)).ToArray();

        #endregion Public Methods
    }
}