using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Properties

        Track Track { get; }

        IEnumerable<Track> Tracks { get; }

        #endregion Public Properties

        #region Public Methods

        void Clear();

        void Initialize(string path);

        void Next();

        void Previous();

        void Select(Track track);

        void Shuffle();

        #endregion Public Methods
    }
}