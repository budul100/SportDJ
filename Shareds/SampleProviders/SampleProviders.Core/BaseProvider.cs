using NAudio.Wave;
using System;

namespace SportDJ.Shareds.SampleProviders.Core;

public abstract class BaseProvider
    : ISampleProvider, IDisposable
{
    #region Private Fields

    private bool isDisposed;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseProvider(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat
            ?? throw new ArgumentNullException(nameof(waveFormat));

        if (WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException(
                message: "Sample provider format must be a floating point format.",
                paramName: nameof(waveFormat));
        }
    }

    #endregion Protected Constructors

    #region Public Properties

    public WaveFormat WaveFormat { get; }

    #endregion Public Properties

    #region Public Methods

    public void Dispose()
    {
        Dispose(
            isDisposing: true);

        GC.SuppressFinalize(
            obj: this);
    }

    public abstract int Read(Span<float> buffer);

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool isDisposing)
    {
        if (!isDisposed)
        {
            if (isDisposing)
            { }

            isDisposed = true;
        }
    }

    #endregion Protected Methods
}