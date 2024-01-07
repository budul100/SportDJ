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

        string GetPath(string appName, string fileName);

        void Load(string filePath);

        void Save();

        #endregion Public Methods
    }
}