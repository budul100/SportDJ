using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
        : IDisposable
    {
        #region Public Properties

        int Counter { get; }

        bool HasPlayeds { get; }

        bool HasStack { get; }

        bool HasUnplayeds { get; }

        bool IsActive { get; }

        bool IsAvailable { get; }

        bool IsEnding { get; }

        bool IsLoop { get; set; }

        bool IsPlaying { get; }

        MixerType MixerType { get; set; }

        double? Progress { get; }

        TimeSpan? Remaining { get; }

        IStackService StackService { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Execute();

        void Initialize(string path, string name, bool savePosition, bool isLoop, MixerType mixerType);

        void Next();

        void Previous();

        void Reset();

        #endregion Public Methods
    }
}