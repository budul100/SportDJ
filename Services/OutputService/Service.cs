using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons;
using SportDJ.Commons.Events.Players;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly List<IPlayerService> foregroundServices = new List<IPlayerService>();
        private readonly Dictionary<string, IPlayerService> playerServices = new Dictionary<string, IPlayerService>();
        private readonly ISettingsService<Session> settingsService;

        private FadingSampleProvider backgroundFader;
        private MixingSampleProvider backgroundMixer;
        private bool isDisposed;
        private bool isReducedBackground;
        private WaveOut output;
        private MixingSampleProvider outputMixer;
        private Output outputOptions;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            ISettingsService<Session> settingsService)
        {
            this.containerProvider = containerProvider;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<PlayerStartedEvent>().Subscribe(
                action: p => ReduceBackground(p),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<PlayerStoppedEvent>().Subscribe(
                action: p => RaiseBackground(p),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public string Name { get; private set; }

        public float Volume
        {
            get
            {
                return outputOptions.Volume;
            }
            set
            {
                if (value < Constants.VolumeMin || value > Constants.VolumeMax)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.Volume)
                {
                    outputOptions.Volume = value;

                    if (output != default)
                    {
                        output.Volume = outputOptions.Volume;
                    }
                }
            }
        }

        public double VolumePortionBackground
        {
            get
            {
                return outputOptions.VolumePortionBackground;
            }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.VolumePortionBackground)
                {
                    outputOptions.VolumePortionBackground = value;

                    if (isReducedBackground)
                    {
                        Lower();
                    }
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

        public IPlayerService Get(string name, bool isMultiSignalPlayer, bool isBackgroundPlayer)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!playerServices.ContainsKey(name))
            {
                var playerService = containerProvider.Resolve<IPlayerService>();

                playerService.Initialize(
                    name: name,
                    waveFormat: output.OutputWaveFormat,
                    isMultiSignalPlayer: isMultiSignalPlayer);

                if (isBackgroundPlayer)
                {
                    backgroundMixer.Add(playerService.Output);
                }
                else
                {
                    outputMixer.Add(playerService.Output);

                    foregroundServices.Add(playerService);
                }

                playerServices.Add(
                    key: name,
                    value: playerService);
            }

            var result = playerServices[name];

            return result;
        }

        public Dictionary<int, string> GetDevices()
        {
            var capabilities = GetCapabilities();

            var result = capabilities.ToDictionary(
                keySelector: c => c.Key,
                elementSelector: c => c.Value.ProductName);

            return result;
        }

        public void Initialize(string name)
        {
            Name = name;
            outputOptions = GetOptions(Name);

            if (!string.IsNullOrWhiteSpace(outputOptions.DeviceName))
            {
                var capabilities = GetCapabilities();

                if (capabilities.Any(c => c.Value.ProductName == outputOptions.DeviceName))
                {
                    var deviceNumber = capabilities
                        .First(c => c.Value.ProductName == outputOptions.DeviceName).Key;

                    Set(
                        deviceNumber: deviceNumber);
                }
            }
        }

        public void Lower()
        {
            if (!isReducedBackground)
            {
                var volume = output.Volume * (float)outputOptions.VolumePortionBackground;

                isReducedBackground = true;

                backgroundFader.Fade(
                    volume: volume,
                    milliSeconds: settingsService.Settings.Options.FadeDurationOnBackground);
            }
        }

        public void Raise()
        {
            isReducedBackground = false;

            backgroundFader.Fade(
                volume: Constants.VolumeMax,
                milliSeconds: settingsService.Settings.Options.FadeDurationOnBackground);
        }

        public void Set(int deviceNumber)
        {
            var capabilities = GetCapabilities();

            if (capabilities.ContainsKey(deviceNumber))
            {
                CloseOutput();

                output = new WaveOut
                {
                    DeviceNumber = deviceNumber,
                    Volume = outputOptions.Volume,
                };

                outputOptions.DeviceName = capabilities[deviceNumber].ProductName;

                backgroundMixer = new MixingSampleProvider(
                    waveFormat: output.OutputWaveFormat);

                backgroundFader = new FadingSampleProvider(
                    sourceProvider: backgroundMixer,
                    volume: Constants.VolumeMax);

                outputMixer = new MixingSampleProvider(
                    waveFormat: output.OutputWaveFormat);

                outputMixer.Add(backgroundFader);

                var waveProvider = new NAudio.Wave.SampleProviders.SampleToWaveProvider(outputMixer);

                output.Init(
                    waveProvider: waveProvider);

                output.Play();
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
            if (output != default)
            {
                output.Stop();

                outputMixer.Dispose();
                backgroundMixer.Dispose();
                output.Dispose();

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

        private Output GetOptions(string name)
        {
            if (!settingsService.Settings.Options.Outputs.Any(m => m.Name == name))
            {
                var outputOptions = new Output
                {
                    Name = name,
                    Volume = Constants.VolumeMax,
                };

                settingsService.Settings.Options.Outputs.Add(outputOptions);
            }

            var result = settingsService.Settings.Options.Outputs
                .Single(s => s.Name == Name);

            return result;
        }

        private void RaiseBackground(IPlayerService playerService)
        {
            if (foregroundServices.Contains(playerService))
            {
                Raise();
            }
        }

        private void ReduceBackground(IPlayerService playerService)
        {
            if (foregroundServices.Contains(playerService))
            {
                Lower();
            }
        }

        #endregion Private Methods
    }
}