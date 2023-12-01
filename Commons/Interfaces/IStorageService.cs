using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IStorageService<T>
        : IDisposable
        where T : class
    {
        #region Public Properties

        T Contents { get; }

        #endregion Public Properties

        #region Public Methods

        void Load(string fileName);

        void Save();

        #endregion Public Methods
    }
}