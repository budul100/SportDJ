using System;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace SportDJ.Commons.Interfaces
{
    public interface IHookService
        : IDisposable
    {
        #region Public Methods

        void Activate(object obj);

        void Deactivate(object obj);

        void Listen(Action<string> callback);

        #endregion Public Methods
    }
}