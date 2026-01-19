using System;
using System.Collections.Generic;
using NAudio.Wave;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces
{
    public interface IDeviceService
        : IDisposable
    {
        #region Public Events

        event EventHandler<EventArgs> DevicesUpdated;

        event EventHandler<StoppedEventArgs> OutputStopped;

        #endregion Public Events

        #region Public Properties

        Device Device { get; }

        IEnumerable<Device> Devices { get; }

        bool IsActive { get; }

        float Volume { get; set; }

        WaveFormat WaveFormat { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize(ISampleProvider sampleProvider, float volume);

        void Play();

        void Set(string deviceName, int sampleRate, bool takeDefaultIfMissing = false);

        void Stop();

        void Update();

        #endregion Public Methods
    }
}