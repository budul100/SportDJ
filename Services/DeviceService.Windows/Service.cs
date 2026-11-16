using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Services.DeviceService.Windows.Exceptions;

namespace SportDJ.Services.DeviceService.Windows;

public class Service(IEventAggregator eventAggregator)
    : IDeviceService
{
    #region Private Fields

    private bool isDisposed;
    private IWavePlayer player;

    #endregion Private Fields

    #region Public Properties

    public Device Device { get; private set; }

    public IEnumerable<Device> Devices { get; private set; } = [];

    public bool IsActive => player != default;

    public float Volume
    {
        get => player?.Volume ?? 0;

        set
        {
            if (player != default)
            {
                player.Volume = value;
            }
        }
    }

    public WaveFormat WaveFormat { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void Dispose()
    {
        Dispose(
            disposing: true);

        GC.SuppressFinalize(
            obj: this);
    }

    public void Initialize(ISampleProvider provider, float volume)
    {
        if (Device != default)
        {
            try
            {
                var outputProvider = provider;

                switch (Device.Type)
                {
                    case DeviceType.Asio:

                        var asioOut = ExecuteOnSTAThread(() => new AsioOut(Device.Name));

                        if (!asioOut.IsSampleRateSupported(outputProvider.WaveFormat.SampleRate))
                        {
                            outputProvider = new WdlResamplingSampleProvider(
                                source: provider,
                                newSampleRate: Constants.SampleRateDefaultAsio);
                        }

                        player = asioOut;
                        break;

                    case DeviceType.Wasapi:

                        if (Device.Number.HasValue)
                        {
                            player = new WaveOut
                            {
                                DeviceNumber = Device.Number.Value
                            };
                        }

                        break;
                }

                if (player != default)
                {
                    var playerProvider = new SampleToWaveProvider(outputProvider);

                    player.PlaybackStopped += OnPlaybackStopped;
                    player.Init(playerProvider);

                    Volume = volume;
                }
            }
            catch
            {
                // TO DO: Log exception
                Debugger.Break();
            }
        }
    }

    public void Play() => player?.Play();

    public void Set(string name, int sampleRate, bool takeDefaultIfMissing = false)
    {
        var device = default(Device);

        if (!string.IsNullOrWhiteSpace(name))
        {
            if (Devices.Any())
            {
                device = Devices.SingleOrDefault(d => d.Name == name);
            }
            else
            {
                throw new NoDeviceAvailableException();
            }
        }

        if (device != Device)
        {
            CloseDevice();

            SetDevice(
                device: device,
                sampleRate: sampleRate,
                takeDefaultIfMissing: takeDefaultIfMissing);
        }
    }

    public void Stop() => player?.Stop();

    public void Update()
    {
        Devices = GetCapabilitiesAsio()
            .Union(GetCapabilitiesWaveOut()).ToArray();

        eventAggregator.GetEvent<DevicesUpdatedEvent>().Publish();
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                CloseDevice();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static T ExecuteOnSTAThread<T>(Func<T> function)
    {
        T result = default;

        var staThread = new Thread(() =>
        {
            try
            {
                result = function();
            }
            catch
            {
                // TO DO: Log exception
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);

        staThread.Start();
        staThread.Join();

        return result;
    }

    private void CloseDevice()
    {
        if (player != default)
        {
            player.Stop();

            player.PlaybackStopped -= OnPlaybackStopped;
            player.Dispose();

            player = null;
        }

        Device = default;
        WaveFormat = default;
    }

    private IEnumerable<Device> GetCapabilitiesAsio()
    {
        var driverNames = ExecuteOnSTAThread(AsioOut.GetDriverNames);

        foreach (var driverName in driverNames)
        {
            var result = Devices.SingleOrDefault(d => d.Type == DeviceType.Asio
                && d.Name == driverName);

            if (result == default)
            {
                try
                {
                    using var capability = ExecuteOnSTAThread(() => new AsioOut(driverName));

                    if (capability?.DriverOutputChannelCount > 0)
                    {
                        var channels = Math.Min(
                            capability.DriverOutputChannelCount,
                            Constants.OutputChannelMax);

                        result = new Device(
                            Name: driverName,
                            Channels: channels,
                            Type: DeviceType.Asio);
                    }
                }
                catch { }
            }

            if (result != default)
            {
                yield return result;
            }
        }
    }

    private IEnumerable<Device> GetCapabilitiesWaveOut()
    {
        for (var index = Constants.IndexAudioDeviceMin; index < WaveOut.DeviceCount; index++)
        {
            var result = default(Device);

            try
            {
                var capability = WaveOut.GetCapabilities(index);

                result = Devices.SingleOrDefault(d => d.Type == DeviceType.Wasapi
                    && d.Name == capability.ProductName);

                if (result == default
                    && capability.Channels > 0)
                {
                    var channels = Math.Min(
                        capability.Channels,
                        Constants.OutputChannelMax);

                    result = new Device(
                        Name: capability.ProductName,
                        Channels: capability.Channels,
                        Type: DeviceType.Wasapi,
                        Number: index);
                }
            }
            catch { }

            if (result != default)
            {
                yield return result;
            }
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        if (e.Exception != default)
        {
            Debugger.Break();

            // TODO: Log exception
            CloseDevice();
        }

        eventAggregator.GetEvent<DeviceStoppedEvent>().Publish();
    }

    private void SetDevice(Device device, int sampleRate, bool takeDefaultIfMissing)
    {
        Device = device;

        if (device == default)
        {
            if (takeDefaultIfMissing)
            {
                if (Devices.Any())
                {
                    Device = Devices.First();
                }
                else
                {
                    throw new NoDeviceAvailableException();
                }
            }
        }

        if (Device != default)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                sampleRate: sampleRate,
                channels: Device.Channels);
        }
        else
        {
            WaveFormat = default;
        }
    }

    #endregion Private Methods
}