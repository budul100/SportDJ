using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Score2Stream.Commons;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.SettingsService
{
    public class Service<T>
        : ISettingsService<T>
        where T : class
    {
        #region Private Fields

        private const string AppName = nameof(SportDJ);

        private static readonly XmlWriterSettings xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true,
        };

        private readonly SemaphoreSlim saveLock = new(1, 1);

        private readonly XmlSerializer serializer;
        private string filePath;
        private string folderPath;
        private bool isDisposed;
        private T settings;

        #endregion Private Fields

        #region Public Constructors

        public Service()
        {
            serializer = new XmlSerializer(typeof(T));
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public T Get()
        {
            if (settings == default)
            {
                Initialize();
            }

            return settings;
        }

        public void Initialize(string fileName = Constants.SettingsFileNameDefault)
        {
            this.folderPath = GetSettingsFolderPath();
            this.filePath = Path.Combine(
                path1: folderPath,
                path2: fileName);

            LoadSettings(filePath);

            if (settings is default(T))
            {
                settings = Activator.CreateInstance<T>();
            }
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

        private static string GetSettingsFolderPath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var result = Path.Combine(
                path1: appDataFolder,
                path2: AppName);

            return result;
        }

        private void LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                SaveSettings();
            }

            using var file = new StreamReader(filePath);

            try
            {
                settings = serializer.Deserialize(file) as T;
            }
            catch { }
        }

        private void SaveSettings()
        {
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
                    o: settings);
            }
            catch
            { }
        }

        private async void SaveSettingsAsync()
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