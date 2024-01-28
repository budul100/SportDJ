using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IStackService
    {
        #region Public Properties

        List<Entry> Entries { get; }

        Entry Entry { get; }

        bool HasPlayeds { get; }

        bool HasUnplayeds { get; }

        int Index { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(string path, string name, bool savePosition);

        void Next();

        void Play();

        void Previous();

        void Reset();

        void Select(int index);

        void Shuffle();

        #endregion Public Methods
    }
}