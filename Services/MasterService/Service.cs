using NAudio.Wave;
using Score2Stream.Commons;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.MasterService
{
    public class Service
        : IMasterService
    {
        #region Private Fields

        private readonly List<IPlayerService> playerServices = new List<IPlayerService>();

        private FadingSampleProvider backgroundFader;
        private MixingSampleProvider backgroundMixer;
        private bool isDisposed;
        private MixingSampleProvider masterMixer;
        private WaveOut masterOutput;

        #endregion Private Fields

        #region Public Properties

        public string Name { get; private set; }

        public float Volume
        {
            get { return masterOutput?.Volume ?? default; }
            set
            {
                if (value < Constants.VolumeMin || value > Constants.VolumeMax)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (masterOutput != default
                    && value != masterOutput.Volume)
                {
                    masterOutput.Volume = value;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public Dictionary<int, string> GetDevices()
        {
            var capabilities = GetCapabilities();

            var result = capabilities.ToDictionary(
                keySelector: c => c.Key,
                elementSelector: c => c.Value.ProductName);

            return result;
        }

        public void Initialize(int deviceNumber, string name)
        {
            var capabilities = GetCapabilities();

            if (capabilities.ContainsKey(deviceNumber))
            {
                CloseOutput();

                this.Name = name;

                masterOutput = new WaveOut
                {
                    DeviceNumber = deviceNumber,
                    Volume = Constants.VolumeMax,
                };

                backgroundMixer = new MixingSampleProvider(
                    waveFormat: masterOutput.OutputWaveFormat);

                backgroundFader = new FadingSampleProvider(
                    sourceProvider: backgroundMixer,
                    volume: Constants.VolumeMax);

                masterMixer = new MixingSampleProvider(
                    waveFormat: masterOutput.OutputWaveFormat);

                masterMixer.Add(backgroundFader);

                var waveProvider = new NAudio.Wave.SampleProviders.SampleToWaveProvider(masterMixer);

                masterOutput.Init(
                    waveProvider: waveProvider);

                masterOutput.Play();

                playerServices.Clear();
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    CloseOutput();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CloseOutput()
        {
            if (masterOutput != default)
            {
                masterOutput.Stop();

                masterMixer.Dispose();
                backgroundMixer.Dispose();
                masterOutput.Dispose();

                playerServices.Clear();
            }
        }

        private Dictionary<int, WaveOutCapabilities> GetCapabilities()
        {
            var result = new Dictionary<int, WaveOutCapabilities>();

            for (var index = -1; index < WaveOut.DeviceCount; index++)
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

        #endregion Private Methods
    }
}