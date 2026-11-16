using System;

namespace SportDJ.Commons.Interfaces;

public interface IStorageService<T>
    : IDisposable
    where T : class
{
    #region Public Properties

    T Contents { get; }

    string Path { get; }

    #endregion Public Properties

    #region Public Methods

    string GetPath(string appName, string fileName,
        Environment.SpecialFolder baseFolder = Environment.SpecialFolder.LocalApplicationData);

    string GetPathFull(string path);

    string GetPathRelative(string path);

    void Load(string filePath);

    void Save(string filePath = default);

    #endregion Public Methods
}