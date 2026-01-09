using System.Collections.Generic;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Methods

        IEnumerable<Track> Get(IEnumerable<string> paths);

        void Play(Track track, bool isPause);

        IEnumerable<Track> Search(string search);

        void Stop(Track track);

        void Update(Track track);

        #endregion Public Methods
    }
}