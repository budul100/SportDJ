using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Exceptions;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Services.OutputService.Windows
{
    internal class Output
        : IDisposable
    {
        #region Private Fields

        private bool isDisposed;
        private IWavePlayer player;

        #endregion Private Fields

        #region Public Constructors

        public Output()
        {
            Update();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<StoppedEventArgs> Stopped;

        #endregion Public Events

        #region Public Properties

        public Device Device { get; private set; }

        public IEnumerable<Device> Devices { get; private set; } = [];

        public bool IsActive { get; private set; }

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

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Initialize(IWaveProvider waveProvider, float volume)
        {
            if (Device != default)
            {
                switch (Device.Type)
                {
                    case DeviceType.Wasapi:

                        player = new WaveOutEvent { DeviceNumber = Device.Number, };
                        break;

                    case DeviceType.Asio:

                        player = ExecuteOnSTAThread(() => new AsioOut(Device.Name));
                        break;
                }

                player.PlaybackStopped += OnPlaybackStopped;
                player.Init(waveProvider);

                Volume = volume;
                IsActive = true;
            }
        }

        public void Play() => player?.Play();

        public void Set(string deviceName, bool takeDefaultIfMissing = false)
        {
            var device = default(Device);

            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                device = Devices.SingleOrDefault(d => d.Name == deviceName);
            }

            if (device != Device)
            {
                DisposePlayer();

                SetDevice(
                    device: device,
                    takeDefaultIfMissing: takeDefaultIfMissing);
            }
        }

        public void Stop() => player?.Stop();

        public void Update()
        {
            Devices = GetCapabilitiesAsio()
                .Union(GetCapabilitiesWaveOut()).ToArray();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    DisposePlayer();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static T ExecuteOnSTAThread<T>(Func<T> function)
        {
            T result = default;
            Exception exception = default;

            var staThread = new Thread(() =>
            {
                try
                {
                    result = function();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);

            staThread.Start();
            staThread.Join();

            return result;
        }

        private void DisposePlayer()
        {
            if (player != default)
            {
                player.Stop();

                player.PlaybackStopped -= OnPlaybackStopped;
                player.Dispose();

                player = null;
            }
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
                                name: driverName,
                                channels: channels,
                                type: DeviceType.Asio);
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
                            name: capability.ProductName,
                            channels: capability.Channels,
                            type: DeviceType.Wasapi,
                            number: index);
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
            Stopped?.Invoke(
                sender: this,
                e: e);
        }

        private void SetDevice(Device device, bool takeDefaultIfMissing)
        {
            Device = device;

            if (device == default)
            {
                if (Devices.Any())
                {
                    if (takeDefaultIfMissing)
                    {
                        Device = Devices.First();
                    }
                }
                else
                {
                    throw new NoDeviceAvailableException();
                }
            }

            IsActive = false;
        }

        #endregion Private Methods
    }
}