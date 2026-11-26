using System;
using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Interfaces;

public interface IMixerService
    : IDisposable
{
    #region Public Properties

    TimeSpan? Duration { get; }

    bool IsEnding { get; }

    bool IsInitialized { get; }

    bool IsLoop { get; }

    bool IsLoopReady { get; }

    bool IsPlaying { get; }

    ISampleProvider Output { get; }

    TimeSpan? Position { get; }

    double? Progress { get; }

    TimeSpan? Remaining { get; }

    Track Track { get; }

    MixerType Type { get; }

    float Volume { get; set; }

    #endregion Public Properties

    #region Public Methods

    void ActivateLoop();

    ISignalService Add(Track track);

    void DeactivateLoop();

    void End();

    void Initialize(MixerType type, WaveFormat waveFormat, bool signalsCanOverlap);

    void Pause();

    /// <summary>
    /// Takes the signal out of the mix. Unknown signals are ignored.
    /// </summary>
    void Remove(ISignalService signalService);

    #endregion Public Methods
}