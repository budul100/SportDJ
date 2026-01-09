using System;
using System.Collections.Generic;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Interfaces
{
    public interface IOutputService
        : IDisposable
    {
        #region Public Properties

        Device Device { get; }

        IEnumerable<Device> Devices { get; }

        bool IsActive { get; }

        bool IsLowered { get; }

        bool IsPlaying { get; }

        OutputType Type { get; }

        float Volume { get; set; }

        WaveFormat WaveFormat { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(IMixerService mixerService, bool onForeground);

        void Initialize(OutputType type);

        void Lower();

        void Pause();

        void Raise();

        void Set(string deviceName, int sampleRate = Defaults.SampleRate, bool takeDefaultIfMissing = false);

        void Update();

        #endregion Public Methods
    }
}