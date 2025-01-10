using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IMidiService
        : IDisposable
    {
        #region Public Properties

        bool IsActive { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate();

        void Deactivate();

        void Mute(int? milliSeconds = default);

        #endregion Public Methods
    }
}