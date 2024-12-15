using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Methods

        void Begin(Track track);

        void End(Track track);

        IEnumerable<Track> Get(IEnumerable<string> paths);

        IEnumerable<Track> Search(string search);

        void Update(Track track);

        #endregion Public Methods
    }
}