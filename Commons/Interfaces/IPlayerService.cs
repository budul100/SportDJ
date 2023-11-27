using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
    {
        #region Public Properties

        bool IsLoop { get; set; }

        IMixerService MixerLive { get; }

        IMixerService MixerPre { get; }

        TimeSpan PositionLive { get; }

        TimeSpan PositionPre { get; }

        TimeSpan RemainingLive { get; }

        TimeSpan RemainingPre { get; }

        StateType StateLive { get; }

        StateType StatePre { get; }

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