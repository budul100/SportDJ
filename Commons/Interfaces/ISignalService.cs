using NAudio.Wave;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface ISignalService
        : IDisposable
    {
        #region Public Properties

        bool IsLoop { get; set; }

        bool IsPlaying { get; }

        bool IsStarted { get; }

        ISampleProvider Output { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(WaveFormat waveFormat, Track track, TimeSpan? begin = default,
            TimeSpan? length = default);

        void Pause();

        void Start();

        void Stop();

        #endregion Public Methods
    }
}