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

        private readonly object readerLock = new();

        private int blockAlign;
        private AudioFileReader fileReader;
        private bool isDisposed;
        private long length;

        #endregion Private Fields

        #region Public Properties

        public long Length => length;

        public long Position
        {
            get
            {
                lock (readerLock)
                {
                    return fileReader?.Position != default
                        ? fileReader.Position / blockAlign
                        : 0;
                }
            }

            set
            {
                lock (readerLock)
                {
                    if (fileReader != default)
                    {
                        var position = Math.Clamp(value, 0, length);
                        fileReader.Position = position * blockAlign;
                    }
                }
            }
        }

        public WaveFormat WaveFormat => fileReader?.WaveFormat;

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

            blockAlign = fileReader.WaveFormat.BlockAlign;
            length = fileReader.Length / blockAlign;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = 0;

            lock (readerLock)
            {
                if (fileReader != default)
                {
                    samplesRead = fileReader.Read(
                        buffer: buffer,
                        offset: offset,
                        count: count);
                }
            }

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
                    lock (readerLock)
                    {
                        fileReader.Dispose();

                        fileReader = default;
                    }
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods
    }
}