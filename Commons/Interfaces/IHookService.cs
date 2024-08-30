using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IHookService
        : IDisposable
    {
        #region Public Properties

        public bool IsAvailable { get; }

        public bool IsRunning { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate(object obj);

        void Deactivate(object obj);

        void Listen(Action<string> callback);

        #endregion Public Methods
    }
}