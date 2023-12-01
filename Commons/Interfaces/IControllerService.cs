using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IControllerService
    {
        #region Public Properties

        bool IsLoop { get; set; }

        IPlayerService MixerLive { get; }

        IPlayerService MixerPre { get; }

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

        void InitializeLive(IPlayerService mixer);

        void InitializePre(IPlayerService mixer);

        void Reset();

        #endregion Public Methods
    }
}