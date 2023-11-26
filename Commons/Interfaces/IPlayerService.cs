using Avalonia.Controls.Primitives;
using SportDJ.Commons.Enums;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
    {
        #region Public Properties

        public int Index { get; }

        public bool IsLoop { get; set; }

        public IMixerService MixerLive { get; }

        public IMixerService MixerPre { get; }

        public TimeSpan PositionLive { get; }

        public TimeSpan PositionPre { get; }

        public TimeSpan RemainingLive { get; }

        public TimeSpan RemainingPre { get; }

        public StateType StateLive { get; }

        public StateType StatePre { get; }

        Track Track { get; }

        IEnumerable<Track> Tracks { get; }

        #endregion Public Properties

        #region Public Methods

        void Backward();

        void Clear();

        void Execute();

        void Forward();

        void InitializeLive(IMixerService mixer);

        void InitializePre(IMixerService mixer);

        void Reset();

        void Shuffle();

        void StartPre();

        void StopPre();

        #endregion Public Methods
    }
}