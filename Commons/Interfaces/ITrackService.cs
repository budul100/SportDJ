using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;
using System.IO;

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

        void Load(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        void Next();

        void Previous();

        void Select(Track track);

        void Shuffle();

        #endregion Public Methods
    }
}