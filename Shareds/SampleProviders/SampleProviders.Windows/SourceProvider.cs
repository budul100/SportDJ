using System;
using System.IO;
using NAudio.Wave;
using SportDJ.Shareds.SampleProviders.Abstractions;

namespace SportDJ.Shareds.SampleProviders.Windows
{
    public class SourceProvider
        : ISourceProvider
    {
        #region Private Fields

        private AudioFileReader fileReader;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Properties

        public long Length => fileReader.Length;

        public long Position
        {
            get => fileReader.Position;
            set => fileReader.Position = value;
        }

        public WaveFormat WaveFormat => fileReader.WaveFormat;

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Initialize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    message: "The audio file was not found.",
                    fileName: filePath);
            }

            fileReader = new(filePath);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = fileReader.Read(
                buffer: buffer,
                offset: offset,
                count: count);

            return samplesRead;
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    fileReader.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods
    }
}