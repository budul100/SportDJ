using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IStackService
    {
        #region Public Properties

        bool HasEntries { get; }

        Track Track { get; }

        IEnumerable<Track> Tracks { get; }

        #endregion Public Properties

        #region Public Methods

        void Clear();

        void Initialize(string path);

        void Next();

        void Play();

        void Previous();

        void Reset();

        void Select(Track track);

        void Shuffle();

        void Stop();

        #endregion Public Methods
    }
}