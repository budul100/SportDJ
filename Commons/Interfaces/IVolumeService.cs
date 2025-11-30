using System;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Interfaces
{
    public interface IVolumeService
        : IDisposable
    {
        #region Public Properties

        bool IsActive { get; }

        bool IsInitialized { get; }

        VolumeType Type { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate();

        void Deactivate();

        void Initialize(string deviceName);

        void Mute(int? milliSeconds = default);

        void Raise(float? volumeTarget = default, int? milliSeconds = default);

        void Reduce(float? volumeTarget = default, int? milliSeconds = default);

        #endregion Public Methods
    }
}