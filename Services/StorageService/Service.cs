using Prism.Events;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SportDJ.Services.StorageService
{
    public class Service<T>
        : IStorageService<T>
        where T : class
    {
        #region Private Fields

        private static readonly XmlWriterSettings xmlSettings = new()
        {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true,
        };

        private readonly IEventAggregator eventAggregator;
        private readonly SemaphoreSlim saveLock = new(1, 1);
        private readonly XmlSerializer serializer = new(typeof(T));

        private string filePath;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public T Contents { get; private set; }

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

            var result = Path.Combine(
                path1: appDataFolder,
                path2: appName,
                path3: fileName);

            return result;
        }

        public void Load(string filePath)
        {
            LoadSettings(filePath);

            if (Contents is default(T))
            {
                Contents = Activator.CreateInstance<T>();
            }

            eventAggregator.GetEvent<SettingsLoadedEvent<T>>().Publish();
        }

        public void Save()
        {
            Task.Run(() => SaveSettingsAsync());
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

        private void LoadSettings(string filePath)
        {
            this.filePath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath))
            {
                SaveSettings();
            }

            using var file = new StreamReader(filePath);

            try
            {
                Contents = serializer.Deserialize(file) as T;
            }
            catch { }
        }

        private void SaveSettings()
        {
            var folderPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var file = File.Create(filePath);

            using var writer = XmlWriter.Create(
                output: file,
                settings: xmlSettings);

            try
            {
                serializer.Serialize(
                    xmlWriter: writer,
                    o: Contents);
            }
            catch
            { }
        }

        private async Task SaveSettingsAsync()
        {
            await saveLock.WaitAsync();

            try
            {
                SaveSettings();
            }
            finally
            {
                saveLock.Release();
            }
        }

        #endregion Private Methods
    }
}