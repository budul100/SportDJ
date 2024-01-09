using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Methods

        int GetCount(Track track);

        IEnumerable<Track> GetTracks(string path);

        void Play(Track track);

        void Render(Track track);

        void Stop(Track track);

        #endregion Public Methods
    }
}