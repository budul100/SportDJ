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

        IEnumerable<Entry> Entries { get; }

        bool HasPaths { get; }

        bool HasPlayeds { get; }

        bool HasStack { get; }

        bool HasUnplayeds { get; }

        bool IsActive { get; }

        bool IsAvailable { get; }

        bool IsEnding { get; }

        bool IsForeground { get; set; }

        bool IsPlaying { get; }

        double? Progress { get; }

        TimeSpan? Remaining { get; }

        bool ResetPosition { get; set; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        Task ClearAsync();

        void Execute();

        void Initialize(Player player);

        void Next();

        void Previous();

        Task RegisterAsync();

        Task SetFileAsync();

        Task SetFolderAsync();

        void Unregister();

        #endregion Public Methods
    }
}