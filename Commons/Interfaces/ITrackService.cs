using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface ITrackService
    {
        #region Public Properties

        public int Index { get; }

        Track Track { get; }

        IEnumerable<Track> Tracks { get; }

        #endregion Public Properties

        #region Public Methods

        void Clear();

        void Next();

        void Previous();

        void Shuffle();

        #endregion Public Methods
    }
}