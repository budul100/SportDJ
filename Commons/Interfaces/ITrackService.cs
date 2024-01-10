using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Methods

        void Begin(Track track);

        void End(Track track);

        int GetCount(Track track);

        IEnumerable<Track> GetTracks(string path);

        void Render(Track track);

        #endregion Public Methods
    }
}