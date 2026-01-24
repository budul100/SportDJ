using System.Collections.Generic;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Structure;

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

        void Initialize(Player player, IEnumerable<string> paths);

        void Next();

        void Play();

        void Previous();

        void Reset();

        void Select(Entry entry);

        void Select(int index);

        void Shuffle();

        #endregion Public Methods
    }
}