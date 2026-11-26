using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Logging;

namespace SportDJ.Services.StorageService;

public class Service<T>(IEventAggregator eventAggregator)
    : IStorageService<T>
    where T : class
{
    #region Private Fields

    private const int WaitingPositions = 2;

    private static readonly XmlWriterSettings xmlSettings = new()
    {
        Indent = true,
        IndentChars = "\t",
        NewLineChars = "\r\n",
        NewLineHandling = NewLineHandling.Replace,
        OmitXmlDeclaration = true,
    };

    private readonly Lock saveLock = new();
    private readonly XmlSerializer serializer = new(typeof(T));

    private readonly SemaphoreSlim waitLock = new(
        initialCount: 1,
        maxCount: WaitingPositions);

    private bool isDisposed;

    #endregion Private Fields

    #region Public Properties

    public T Contents { get; private set; }

    public string Path { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void Dispose()
    {
        Dispose(
            disposing: true);

        GC.SuppressFinalize(
            obj: this);
    }

    public string GetPath(string appName, string fileName,
        Environment.SpecialFolder baseFolder = Environment.SpecialFolder.LocalApplicationData)
    {
        var appDataFolder = Environment.GetFolderPath(baseFolder);

        var result = System.IO.Path.Combine(
            path1: appDataFolder,
            path2: appName,
            path3: fileName);

        return result;
    }

    public string GetPathFull(string path)
    {
        return ResolvePath(
            path: path,
            folder: GetFolder(Path));
    }

    public string GetPathRelative(string path)
    {
        if (!string.IsNullOrWhiteSpace(Path)
            && Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
        {
            return System.IO.Path.GetRelativePath(
                relativeTo: System.IO.Path.GetDirectoryName(Path),
                path: path);
        }
        else
        {
            return path;
        }
    }

    public void Load(string filePath)
    {
        var settingsLoaded = LoadSettings(filePath);

        if (Contents is default(T))
        {
            Contents = Activator.CreateInstance<T>();

            settingsLoaded = true;
        }

        if (settingsLoaded)
        {
            eventAggregator.GetEvent<SettingsLoadedEvent<T>>().Publish();
        }
    }

    public void New(string filePath)
    {
        Contents = Activator.CreateInstance<T>();

        // From now on all changes go to the new path, so the previous file stays untouched
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            Path = System.IO.Path.GetFullPath(filePath);
        }

        eventAggregator.GetEvent<SettingsLoadedEvent<T>>().Publish();
    }

    public void Save(string filePath = default)
    {
        Task.Run(() => SaveSettingsAsync(filePath));
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                SaveSettings();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static string GetFolder(string filePath)
    {
        var result = !string.IsNullOrWhiteSpace(filePath)
            ? System.IO.Path.GetDirectoryName(filePath)
            : default;

        return Directory.Exists(result)
            ? result
            : default;
    }

    // Makes the path relative to the new folder and normalizes it. On another drive it stays absolute.
    private static string RebasePath(string path, string oldFolder, string newFolder)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var resolved = ResolvePath(
            path: path,
            folder: oldFolder);

        // Without a known base folder a relative path cannot be converted
        return System.IO.Path.IsPathRooted(resolved)
            ? System.IO.Path.GetRelativePath(
                relativeTo: newFolder,
                path: resolved)
            : path;
    }

    private static string ResolvePath(string path, string folder)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // Without a settings folder a relative path stays as it is
        if (System.IO.Path.IsPathRooted(path))
        {
            return System.IO.Path.GetFullPath(path);
        }
        else if (!string.IsNullOrWhiteSpace(folder))
        {
            return System.IO.Path.GetFullPath(
                path: System.IO.Path.Combine(folder, path));
        }
        else
        {
            return path;
        }
    }

    private bool LoadSettings(string filePath)
    {
        var result = false;

        var fullPath = !string.IsNullOrWhiteSpace(filePath)
            ? System.IO.Path.GetFullPath(filePath)
            : Path;

        if (!File.Exists(fullPath))
        {
            SaveSettings(fullPath);
        }

        using var file = new StreamReader(fullPath);

        try
        {
            Contents = serializer.Deserialize(file) as T;

            result = true;
        }
        catch (Exception exception)
        {
            Log.Error($"Loading the settings from '{fullPath}' failed.", exception);

            Debugger.Break();
        }

        if (result || string.IsNullOrWhiteSpace(Path))
        {
            Path = fullPath;
        }

        return result;
    }

    // Returns true if the settings file moves to another folder, which has to be undone if saving fails
    private bool RebasePaths(string oldFolder, string newFolder)
    {
        var result = false;

        if (Contents is IPathHolder holder
            && !string.IsNullOrWhiteSpace(newFolder))
        {
            holder.ConvertPaths(p => RebasePath(p, oldFolder, newFolder));

            result = !string.IsNullOrWhiteSpace(oldFolder)
                && !string.Equals(
                    a: System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(oldFolder)),
                    b: System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(newFolder)),
                    comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private void SaveSettings(string filePath = default)
    {
        var fullPath = !string.IsNullOrWhiteSpace(filePath)
            ? System.IO.Path.GetFullPath(filePath)
            : Path;

        var folderPath = System.IO.Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // The paths are always stored normalized and relative to the settings file, they follow it to a new folder
        var previousFolder = GetFolder(Path);

        var isRebased = RebasePaths(
            oldFolder: previousFolder,
            newFolder: folderPath);

        using var file = File.Create(fullPath);

        using var writer = XmlWriter.Create(
            output: file,
            settings: xmlSettings);

        try
        {
            serializer.Serialize(
                xmlWriter: writer,
                o: Contents);

            Path = fullPath;
        }
        catch (Exception exception)
        {
            if (isRebased)
            {
                RebasePaths(
                    oldFolder: folderPath,
                    newFolder: previousFolder);
            }

            Log.Error($"Saving the settings to '{fullPath}' failed.", exception);

            Debugger.Break();
        }
    }

    private async Task SaveSettingsAsync(string filePath)
    {
        if (waitLock.CurrentCount < WaitingPositions)
        {
            await waitLock.WaitAsync();

            try
            {
                lock (saveLock)
                {
                    SaveSettings(filePath);
                }
            }
            finally
            {
                waitLock.Release();
            }
        }
    }

    #endregion Private Methods
}
