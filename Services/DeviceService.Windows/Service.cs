using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Logging;
using SportDJ.Commons.Models.Records;
using SportDJ.Services.DeviceService.Windows.Exceptions;

namespace SportDJ.Services.DeviceService.Windows;

public class Service(IEventAggregator eventAggregator)
    : IDeviceService
{
    #region Private Fields

    // Tried in this order, after the wanted sample rate and the default one
    private static readonly int[] SampleRatesAsio = [44100, 48000, 96000, 88200, 192000, 176400, 32000];

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

    public bool Initialize(ISampleProvider provider, float volume)
    {
        if (Device == default)
        {
            return false;
        }

        var newPlayer = default(IWavePlayer);

        try
        {
            var outputProvider = provider;

            switch (Device.Type)
            {
                case DeviceType.Asio:
                    {
                        var asioOut = ExecuteOnSTAThread(() => new AsioOut(Device.Name))
                            ?? throw new InvalidOperationException($"The ASIO driver '{Device.Name}' cannot be opened.");

                        newPlayer = asioOut;

                        outputProvider = GetAsioProvider(
                            asioOut: asioOut,
                            deviceName: Device.Name,
                            provider: provider);

                        break;
                    }

                case DeviceType.Wasapi:

                    if (Device.Number.HasValue)
                    {
                        newPlayer = new WaveOut
                        {
                            DeviceNumber = Device.Number.Value
                        };
                    }

                    break;
            }

            if (newPlayer == default)
            {
                throw new InvalidOperationException($"There is no player for the device '{Device.Name}'.");
            }

            newPlayer.Init(new SampleToWaveProvider(outputProvider));
            newPlayer.PlaybackStopped += OnPlaybackStopped;

            // Only a player which is initialized is taken over, otherwise Play() would start a driver without buffers
            player = newPlayer;

            Volume = volume;

            return true;
        }
        catch (Exception exception)
        {
            Log.Error($"Initializing the device '{Device?.Name}' failed.", exception);

            DisposePlayer(newPlayer);

            // A device which cannot be used must not stay selected
            CloseDevice();

            return false;
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

    private static void DisposePlayer(IWavePlayer wavePlayer)
    {
        try
        {
            wavePlayer?.Dispose();
        }
        catch (Exception exception)
        {
            // A driver which failed to initialize often fails here as well
            Log.Debug($"Releasing the player failed: {exception.Message}");
        }
    }

    private static T ExecuteOnSTAThread<T>(Func<T> function)
    {
        T result = default;

        var staThread = new Thread(() =>
        {
            try
            {
                result = function();
            }
            catch (Exception exception)
            {
                // Drivers fail for many reasons (in use, not connected, wrong bitness), so this is no error of the app
                Log.Warning("Accessing the audio driver failed.", exception);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);

        staThread.Start();
        staThread.Join();

        return result;
    }

    // The provider is resampled, if the driver does not support its sample rate
    private static ISampleProvider GetAsioProvider(AsioOut asioOut, string deviceName, ISampleProvider provider)
    {
        var sampleRate = GetAsioSampleRate(
            asioOut: asioOut,
            deviceName: deviceName,
            sampleRateWanted: provider.WaveFormat.SampleRate);

        if (sampleRate == provider.WaveFormat.SampleRate)
        {
            return provider;
        }

        return new WdlResamplingSampleProvider(
            source: provider,
            newSampleRate: sampleRate);
    }

    // The driver decides, so the default sample rate is no guarantee either
    private static int GetAsioSampleRate(AsioOut asioOut, string deviceName, int sampleRateWanted)
    {
        var candidates = new[] { sampleRateWanted, Constants.SampleRateDefaultAsio }
            .Concat(SampleRatesAsio)
            .Distinct().ToArray();

        var supporteds = candidates
            .Where(asioOut.IsSampleRateSupported).ToArray();

        Log.Debug($"The ASIO driver '{deviceName}' supports the sample rates: {string.Join(", ", supporteds)}");

        if (supporteds.Length == 0)
        {
            throw new NotSupportedException(
                $"The ASIO driver '{deviceName}' supports none of the sample rates {string.Join(", ", candidates)}.");
        }

        return supporteds[0];
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
        var driverNames = ExecuteOnSTAThread(AsioOut.GetDriverNames) ?? [];

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
                catch (Exception exception)
                {
                    Log.Debug($"The ASIO driver '{driverName}' is not usable: {exception.Message}");
                }
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
                        Channels: channels,
                        Type: DeviceType.Wasapi,
                        Number: index);
                }
            }
            catch (Exception exception)
            {
                Log.Debug($"The output device {index} is not usable: {exception.Message}");
            }

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
            Log.Error($"The playback on '{Device?.Name}' stopped with an error.", e.Exception);

            Debugger.Break();

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
