using System.Collections.Generic;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Methods

        void Begin(Track track);

        void End(Track track);

        int GetCount(Track track);

        IEnumerable<Track> GetTracks(IEnumerable<string> paths);

        void Render(Track track);

        #endregion Public Methods
    }
}