using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using SportDJ.Shareds.SampleProviders.Abstractions;

namespace SportDJ.Shareds.SampleProviders.Windows;

public class SourceProvider
    : ISourceProvider
{
    #region Private Fields

    private int blockAlign;
    private AudioFileReader fileReader;
    private bool isDisposed;
    private long length;
    private readonly Lock readerLock = new();

    #endregion Private Fields

    #region Public Properties

    public long Length => length;

    public long Position
    {
        get
        {
            lock (readerLock)
            {
                return fileReader != default
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

        var reader = new AudioFileReader(filePath);

        lock (readerLock)
        {
            // A second initialization must not leak the previous file handle
            fileReader?.Dispose();

            fileReader = reader;

            blockAlign = reader.WaveFormat.BlockAlign;
            length = reader.Length / blockAlign;
        }
    }

    public int Read(Span<float> buffer)
    {
        var samplesRead = 0;

        lock (readerLock)
        {
            if (fileReader != default)
            {
                samplesRead = fileReader.Read(buffer);
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
                    // Null when Initialize was never called
                    fileReader?.Dispose();

                    fileReader = default;
                }
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods
}
