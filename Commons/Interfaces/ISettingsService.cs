using System;

namespace SportDJ.Commons.Interfaces
{
    public interface ISettingsService<T>
        : IDisposable
        where T : class
    {
        #region Public Methods

        T Get();

        void Initialize(string fileName);

        void Save();

        #endregion Public Methods
    }
}