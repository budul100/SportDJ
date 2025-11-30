using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Exceptions;

namespace SportDJ.Services.OutputService.Windows
{
    internal class OutputProvider
    {
        #region Private Fields

        private WaveOutEvent waveOutEvent;

        #endregion Private Fields

        #region Public Events

        public event EventHandler<StoppedEventArgs> Stopped;

        #endregion Public Events

        #region Public Properties

        public int Channels { get; private set; }

        public string DeviceName { get; private set; }

        public int? DeviceNumber { get; private set; }

        public Dictionary<int, string> Devices { get; private set; }

        public bool IsActive { get; private set; }

        public float Volume
        {
            get => waveOutEvent?.Volume ?? 0;

            set
            {
                if (waveOutEvent != default)
                {
                    waveOutEvent.Volume = value;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(int deviceNumber, IWaveProvider waveProvider, float volume)
        {
            waveOutEvent = new WaveOutEvent { DeviceNumber = deviceNumber };
            waveOutEvent.PlaybackStopped += OnPlaybackStopped;
            waveOutEvent.Init(waveProvider);

            Volume = volume;
            IsActive = true;
        }

        public void Play() => waveOutEvent?.Play();

        public void Set(string deviceName, bool takeDefaultIfMissing = false)
        {
            if (!IsActive
                || DeviceName != deviceName)
            {
                DeviceNumber = default;
                DeviceName = default;
                Channels = default;

                var capabilities = GetCapabilities();

                try
                {
                    DeviceNumber = GetDeviceNumber(
                        capabilities: capabilities,
                        deviceName: deviceName,
                        takeDefaultIfMissing: takeDefaultIfMissing);
                }
                catch (NoDeviceAvailableException)
                { }

                if (DeviceNumber.HasValue)
                {
                    DeviceName = capabilities[DeviceNumber.Value].ProductName;
                    Channels = capabilities[DeviceNumber.Value].Channels;
                }
            }
        }

        public void Stop() => waveOutEvent?.Stop();

        public void Update()
        {
            var capabilities = GetCapabilities();

            Devices = capabilities.ToDictionary(
                keySelector: c => c.Key,
                elementSelector: c => c.Value.ProductName);
        }

        #endregion Public Methods

        #region Private Methods

        private static Dictionary<int, WaveOutCapabilities> GetCapabilities()
        {
            var result = new Dictionary<int, WaveOutCapabilities>();

            for (var index = Constants.IndexAudioDeviceMin; index < WaveOut.DeviceCount; index++)
            {
                var capabilities = WaveOut.GetCapabilities(index);

                if (capabilities.Channels == 1 || capabilities.Channels == 2)
                {
                    result.Add(
                        key: index,
                        value: capabilities);
                }
            }

            return result;
        }

        private static int GetDeviceNumber(Dictionary<int, WaveOutCapabilities> capabilities,
            string deviceName, bool takeDefaultIfMissing)
        {
            var result = default(int?);

            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                result = capabilities
                    .FirstOrDefault(c => c.Value.ProductName == deviceName).Key;
            }

            if (!result.HasValue)
            {
                if (takeDefaultIfMissing)
                {
                    result = capabilities
                        .FirstOrDefault().Key;
                }
                else
                {
                    throw new NoDeviceAvailableException(
                        message: $"There is no device {deviceName} available.");
                }
            }

            if (!result.HasValue)
            {
                throw new NoDeviceAvailableException();
            }

            return result.Value;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Stopped?.Invoke(
                sender: this,
                e: e);
        }

        #endregion Private Methods
    }
}