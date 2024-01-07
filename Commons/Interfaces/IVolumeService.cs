using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IVolumeService
        : IDisposable
    {
        #region Public Properties

        bool IsActive { get; }

        VolumeType VolumeType { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate();

        void Deactivate();

        void Raise(float? volumeTarget = default, int? milliSeconds = default);

        void Reduce(float? volumeTarget = default, int? milliSeconds = default);

        void Set(string deviceName);

        #endregion Public Methods
    }
}