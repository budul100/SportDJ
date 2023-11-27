using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
    {
        #region Public Properties

        public bool IsLoop { get; set; }

        public IMixerService MixerLive { get; }

        public IMixerService MixerPre { get; }

        public TimeSpan PositionLive { get; }

        public TimeSpan PositionPre { get; }

        public TimeSpan RemainingLive { get; }

        public TimeSpan RemainingPre { get; }

        public StateType StateLive { get; }

        public StateType StatePre { get; }

        #endregion Public Properties

        #region Public Methods

        void ExecuteLive();

        void ExecutePre();

        void InitializeLive(IMixerService mixer);

        void InitializePre(IMixerService mixer);

        void Reset();

        #endregion Public Methods
    }
}