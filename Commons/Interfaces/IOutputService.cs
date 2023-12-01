using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IOutputService
        : IDisposable
    {
        #region Public Properties

        string Name { get; }

        float Volume { get; set; }

        double VolumePortionBackground { get; set; }

        #endregion Public Properties

        #region Public Methods

        IPlayerService Get(string name, bool isMultiSignalPlayer, bool isBackgroundPlayer);

        Dictionary<int, string> GetDevices();

        void Initialize(string name);

        void Lower();

        void Raise();

        void Set(int deviceNumber);

        #endregion Public Methods
    }
}