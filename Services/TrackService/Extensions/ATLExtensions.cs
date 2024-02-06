using System.IO;
using System.Text;

namespace SportDJ.Services.TrackService.Extensions
{
    internal static class ATLExtensions
    {
        #region Private Fields

        private const string InfoDelimiter = " - ";

        #endregion Private Fields

        #region Public Methods

        public static string GetInfo(this ATL.Track atlTrack)
        {
            var content = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(atlTrack.Artist))
            {
                content.Append(atlTrack.Artist);
            }
            if (!string.IsNullOrWhiteSpace(atlTrack.Title))
            {
                if (content.Length > 0)
                {
                    content.Append(InfoDelimiter);
                }

                content.Append(atlTrack.Title);
            }

            var result = content.ToString();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = Path.GetFileNameWithoutExtension(atlTrack.Path);
            }

            return result;
        }

        #endregion Public Methods
    }
}