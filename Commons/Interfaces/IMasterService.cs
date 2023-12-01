using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IMasterService
        : IDisposable
    {
        #region Public Properties

        string Name { get; }

        float Volume { get; set; }

        #endregion Public Properties

        #region Public Methods

        Dictionary<int, string> GetDevices();

        void Initialize(int deviceNumber, string name);

        #endregion Public Methods
    }
}