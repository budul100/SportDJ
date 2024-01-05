using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IStackService
    {
        #region Public Properties

        List<Entry> Entries { get; }

        bool HasUnplayeds { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(string path, string controllerName, bool savePosition);

        void Next();

        void Play();

        void Previous();

        void Reset();

        void Select(int index);

        void Shuffle();

        #endregion Public Methods
    }
}