using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.StackService.Extensions
{
    internal static class EntryExtensions
    {
        #region Public Methods

        public static bool AllPlayed(this IEnumerable<Entry> entries)
        {
            var result = entries?
                .All(t => t.Played) == true;

            return result;
        }

        public static bool HasUnplayeds(this IEnumerable<Entry> entries, Track track)
        {
            var result = entries?.Count(t => !t.Played) > 1
                || (track != default && entries?.SingleOrDefault(t => !t.Played)?.Track != track);

            return result;
        }

        public static void Reset(this IEnumerable<Entry> entries)
        {
            if (entries?.Any() == true)
            {
                foreach (var entry in entries)
                {
                    entry.Played = false;
                }
            }
        }

        #endregion Public Methods
    }
}