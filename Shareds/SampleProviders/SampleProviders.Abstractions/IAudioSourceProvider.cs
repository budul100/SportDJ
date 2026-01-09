using System;
using NAudio.Wave;

namespace SportDJ.Shareds.SampleProviders.Abstractions
{
    public interface IAudioSourceProvider
        : ISampleProvider, IDisposable
    {
        #region Public Properties

        long Length { get; }

        long Position { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(string filePath);

        #endregion Public Methods
    }
}