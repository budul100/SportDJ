using System;
using System.Collections.Generic;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces
{
    public interface IControllerService
        : IDisposable
    {
        #region Public Properties

        Controller Controller { get; }

        IEnumerable<Controller> Controllers { get; }

        #endregion Public Properties

        #region Public Methods

        void Update();

        #endregion Public Methods
    }
}