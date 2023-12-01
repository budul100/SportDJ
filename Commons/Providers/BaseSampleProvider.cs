using NAudio.Wave;
using System;

namespace SportDJ.Commons.Providers
{
    public abstract class BaseSampleProvider
        : ISampleProvider, IDisposable
    {
        #region Protected Fields

        protected readonly int averageBytesPerSecond;

        protected ISampleProvider sourceProvider;

        #endregion Protected Fields

        #region Private Fields

        private bool isDisposed;

        #endregion Private Fields

        #region Protected Constructors

        protected BaseSampleProvider(ISampleProvider sourceProvider)
            : this(waveFormat: sourceProvider.WaveFormat)
        {
            this.sourceProvider = sourceProvider;
        }

        protected BaseSampleProvider(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));

            averageBytesPerSecond = WaveFormat.AverageBytesPerSecond;
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

            GC.SuppressFinalize(this);
        }

        public abstract int Read(float[] buffer, int offset, int count);

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
}