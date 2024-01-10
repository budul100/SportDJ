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
                .All(e => !e.CanBePlayed()) == true;

            return result;
        }

        public static bool HasUnplayeds(this IEnumerable<Entry> entries, Track track)
        {
            var result = entries?.Count(e => e.CanBePlayed()) > 1
                || (track != default && entries?.SingleOrDefault(e => e.CanBePlayed())?.Track != track);

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

        #region Private Methods

        private static bool CanBePlayed(this Entry entry)
        {
            return !entry.Played
                && !entry.Track.IsSilent;
        }

        #endregion Private Methods
    }
}