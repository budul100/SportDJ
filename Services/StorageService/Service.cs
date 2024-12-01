using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Prism.Events;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.StorageService
{
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

        private readonly object saveLock = new();
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
            if (!string.IsNullOrWhiteSpace(Path)
                && Directory.Exists(System.IO.Path.GetDirectoryName(Path))
                && !(Directory.Exists(path) || File.Exists(path)))
            {
                return System.IO.Path.Combine(
                    path1: System.IO.Path.GetDirectoryName(Path),
                    path2: path);
            }
            else
            {
                return path;
            }
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
                Path = fullPath;

                result = true;
            }
            catch (Exception exception)
            {
                Debugger.Break();
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
}