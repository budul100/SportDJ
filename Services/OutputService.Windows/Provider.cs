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
    internal class Provider
        : IDisposable
    {
        #region Private Fields

        private bool isDisposed;
        private IWavePlayer player;

        #endregion Private Fields

        #region Public Constructors

        public Provider()
        {
            Update();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<StoppedEventArgs> Stopped;

        #endregion Public Events

        #region Public Properties

        public Device Device { get; private set; }

        public Dictionary<string, Device> Devices { get; } = [];

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
                Devices.TryGetValue(
                    key: deviceName,
                    value: out device);
            }

            if (device != Device)
            {
                DisposePlayer();

                SetPlayer(
                    device: device,
                    takeDefaultIfMissing: takeDefaultIfMissing);
            }
        }

        public void Stop() => player?.Stop();

        public void Update()
        {
            Devices.Clear();

            UpdateCapabilitiesAsio();
            UpdateCapabilitiesWaveOut();
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

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Stopped?.Invoke(
                sender: this,
                e: e);
        }

        private void SetPlayer(Device device, bool takeDefaultIfMissing)
        {
            Device = device;

            if (device == default)
            {
                if (Devices.Count > 0)
                {
                    if (takeDefaultIfMissing)
                    {
                        Device = Devices.Values.First();
                    }
                }
                else
                {
                    throw new NoDeviceAvailableException();
                }
            }

            IsActive = false;
        }

        private void UpdateCapabilitiesAsio()
        {
            var driverNames = ExecuteOnSTAThread(AsioOut.GetDriverNames);

            foreach (var driverName in driverNames)
            {
                try
                {
                    using var capability = ExecuteOnSTAThread(() => new AsioOut(driverName));

                    if (capability?.DriverOutputChannelCount > 0
                        && !Devices.ContainsKey(driverName))
                    {
                        var channels = Math.Min(
                            capability.DriverOutputChannelCount,
                            Constants.OutputChannelMax);

                        var result = new Device(
                            name: driverName,
                            channels: channels,
                            type: DeviceType.Asio);

                        Devices.Add(
                            key: driverName,
                            value: result);
                    }
                }
                catch { }
            }
        }

        private void UpdateCapabilitiesWaveOut()
        {
            for (var index = Constants.IndexAudioDeviceMin; index < WaveOut.DeviceCount; index++)
            {
                try
                {
                    var capability = WaveOut.GetCapabilities(index);

                    if (capability.Channels > 0
                        && !Devices.ContainsKey(capability.ProductName))
                    {
                        var channels = Math.Min(
                            capability.Channels,
                            Constants.OutputChannelMax);

                        var result = new Device(
                            name: capability.ProductName,
                            channels: capability.Channels,
                            type: DeviceType.Wasapi,
                            number: index);

                        Devices.Add(
                            key: capability.ProductName,
                            value: result);
                    }
                }
                catch { }
            }
        }

        #endregion Private Methods
    }
}