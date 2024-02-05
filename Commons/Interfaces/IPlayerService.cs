using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
        : IDisposable
    {
        #region Public Properties

        int Counter { get; }

        IEnumerable<Entry> Entries { get; }

        bool HasPaths { get; }

        bool HasPlayeds { get; }

        bool HasStack { get; }

        bool HasUnplayeds { get; }

        bool IsActive { get; }

        bool IsAvailable { get; }

        bool IsEnding { get; }

        bool IsForeground { get; set; }

        bool IsLoop { get; set; }

        bool IsPlaying { get; }

        string KeyCode { get; set; }

        string Name { get; set; }

        double? Progress { get; }

        TimeSpan? Remaining { get; }

        bool SavePosition { get; set; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        Task ClearAsync();

        void Execute();

        void Initialize(Player player);

        void Next();

        void Previous();

        void Reset();

        Task SetFileAsync();

        Task SetFolderAsync();

        #endregion Public Methods
    }
}