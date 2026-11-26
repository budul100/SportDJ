using System;
using System.Collections.Generic;
using NAudio.Wave;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces;

public interface IDeviceService
    : IDisposable
{
    #region Public Properties

    Device Device { get; }

    IEnumerable<Device> Devices { get; }

    bool IsActive { get; }

    float Volume { get; set; }

    WaveFormat WaveFormat { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Opens the selected device for the given provider. Returns false, if the device cannot be used.
    /// In this case no device is selected anymore.
    /// </summary>
    bool Initialize(ISampleProvider provider, float volume);

    void Play();

    void Set(string name, int sampleRate, bool takeDefaultIfMissing = false);

    void Stop();

    void Update();

    #endregion Public Methods
}
