using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.StackService.Extensions
{
    internal static class TrackExtensions
    {
        #region Public Methods

        public static IEnumerable<Track> GetTracks(this IEnumerable<Track> tracks)
        {
            if (tracks?.Any() == true)
            {
                tracks.Last().Next = tracks.First();

                var previous = tracks.Last();

                foreach (var track in tracks)
                {
                    track.Previous = previous;
                    previous.Next = track;

                    yield return track;

                    previous = track;
                }
            }
        }

        #endregion Public Methods
    }
}