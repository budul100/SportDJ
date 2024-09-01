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

        TimeSpan? Duration { get; }

        bool IsEnding { get; }

        bool IsLoop { get; set; }

        bool IsOverNextThreshold { get; }

        bool IsPlaying { get; }

        bool IsSilent { get; }

        ISampleProvider Output { get; }

        TimeSpan? Position { get; }

        double? Progress { get; }

        TimeSpan? Remaining { get; }

        StateType State { get; }

        Track Track { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(Track track, WaveFormat waveFormat);

        void Pause();

        void Start();

        void Stop();

        #endregion Public Methods
    }
}