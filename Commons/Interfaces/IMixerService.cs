using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IMixerService
        : IDisposable
    {
        #region Public Properties

        bool IsLoop { get; }

        bool IsPlaying { get; }

        ISampleProvider Output { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        Track Track { get; }

        MixerType Type { get; }

        float Volume { get; set; }

        #endregion Public Properties

        #region Public Methods

        ISignalService Add(Track track);

        void Initialize(MixerType type, WaveFormat waveFormat, bool playsMultipleSignals);

        void Pause();

        #endregion Public Methods
    }
}