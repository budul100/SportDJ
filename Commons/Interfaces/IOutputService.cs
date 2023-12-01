using NAudio.Wave;
using Score2Stream.Commons;
using SportDJ.Commons.Enums;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IOutputService
        : IDisposable
    {
        #region Public Properties

        WaveFormat Format { get; }

        OutputType Type { get; }

        float Volume { get; set; }

        double VolumePortionBackground { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Add(IPlayerService playerService, bool isForegroundPlayer);

        Dictionary<int, string> GetDevices();

        void Initialize(OutputType type);

        void Lower();

        void Raise();

        void Set(int deviceNumber, int sampleRate = Constants.SampleRateDefault);

        #endregion Public Methods
    }
}