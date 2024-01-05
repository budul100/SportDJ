using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.StackService.Extensions
{
    internal static class TrackExtensions
    {
        #region Public Methods

        public static IEnumerable<Entry> GetEntries(this IEnumerable<Track> tracks)
        {
            if (tracks?.Any() == true)
            {
                foreach (var track in tracks)
                {
                    var result = new Entry
                    {
                        Track = track,
                    };

                    yield return result;
                }
            }
        }

        #endregion Public Methods
    }
}