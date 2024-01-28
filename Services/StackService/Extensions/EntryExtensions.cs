using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.StackService.Extensions
{
    internal static class EntryExtensions
    {
        #region Public Methods

        public static bool AllPlayed(this IEnumerable<Entry> entries, Entry current)
        {
            var result = entries?.All(e => !e.CanBePlayed()) == true
                || !entries.HasUnplayeds(current);

            return result;
        }

        public static bool HasPlayeds(this IEnumerable<Entry> entries)
        {
            var result = entries?.Any(e => !e.Played) == true;

            return result;
        }

        public static bool HasUnplayeds(this IEnumerable<Entry> entries, Entry current)
        {
            var result = entries?.Count(e => e.CanBePlayed()) > 1
                || (current != default && entries?.SingleOrDefault(e => e.CanBePlayed()) != current);

            return result;
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