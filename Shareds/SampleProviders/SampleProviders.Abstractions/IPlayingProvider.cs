using System;
using NAudio.Wave;

namespace SportDJ.Shareds.SampleProviders.Abstractions
{
    public interface IPlayingProvider
        : ISampleProvider, IDisposable
    {
        #region Public Properties

        long Length { get; }

        long Position { get; set; }

        #endregion Public Properties
    }
}