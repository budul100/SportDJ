using NAudio.Wave;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface IPlayerService
        : IDisposable
    {
        #region Public Properties

        bool IsLoop { get; }

        bool IsMultiSignalPlayer { get; }

        string Name { get; }

        ISampleProvider OutputProvider { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        ISignalService SignalService { get; }

        Track Track { get; }

        float Volume { get; set; }

        #endregion Public Properties

        #region Public Methods

        ISignalService Add(Track track);

        void Initialize(WaveFormat waveFormat, string name, bool isMultiSignalPlayer);

        void Pause();

        #endregion Public Methods
    }
}