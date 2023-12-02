using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface ISignalService
        : IDisposable
    {
        #region Public Properties

        bool IsLoop { get; set; }

        bool IsNextThresholdExceeded { get; }

        bool IsSilent { get; }

        bool IsStarted { get; }

        ISampleProvider Output { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        StateType State { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(Track track, WaveFormat waveFormat, TimeSpan? begin = default,
            TimeSpan? length = default);

        void Pause();

        void Start();

        void Stop();

        #endregion Public Methods
    }
}