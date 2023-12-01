using NAudio.Wave;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
        : IDisposable
    {
        #region Public Properties

        bool IsLoop { get; }

        ISampleProvider Output { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        Track Track { get; }

        PlayerType Type { get; }

        float Volume { get; set; }

        #endregion Public Properties

        #region Public Methods

        ISignalService Add(Track track);

        void Initialize(PlayerType type, WaveFormat waveFormat, bool isMultiSignalPlayer);

        void Pause();

        #endregion Public Methods
    }
}