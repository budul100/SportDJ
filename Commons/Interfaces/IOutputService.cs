using NAudio.Wave;
using SportDJ.Commons.Enums;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Interfaces
{
    public interface IOutputService
        : IDisposable
    {
        #region Public Properties

        string DeviceName { get; }

        WaveFormat Format { get; }

        bool IsPlaying { get; }

        OutputType Type { get; }

        float Volume { get; set; }

        double VolumePortionBackground { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Add(IMixerService mixerService, bool onForeground);

        Dictionary<int, string> GetDevices();

        void Initialize(OutputType type);

        void Lower();

        void Pause();

        void Raise();

        void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false);

        #endregion Public Methods
    }
}