using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Players;
using SportDJ.Commons.Exceptions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;

namespace SportDJ.Services.OutputService
{
    public class Service
        : IOutputService
    {
        #region Private Fields

        private readonly List<IPlayerService> foregroundServices = new List<IPlayerService>();
        private readonly List<IPlayerService> playerServices = new List<IPlayerService>();
        private readonly IStorageService<Set> settingsService;

        private FadedControlProvider backgroundFader;
        private ContinuousMixingProvider backgroundMixer;
        private bool isDisposed;
        private bool isReducedBackground;
        private WaveOut output;
        private ContinuousMixingProvider outputMixer;
        private Output outputOptions;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        {
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

        public WaveFormat Format { get; private set; }

        public OutputType Type { get; private set; }

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

        public void Add(IPlayerService playerService, bool isForegroundPlayer)
        {
            if (isForegroundPlayer)
            {
                outputMixer.Add(playerService.Output);

                foregroundServices.Add(playerService);
            }
            else
            {
                backgroundMixer.Add(playerService.Output);
            }

            playerServices.Add(playerService);
        }

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

        public void Initialize(OutputType type)
        {
            Type = type;

            outputOptions = GetOptions(type);

            if (!string.IsNullOrWhiteSpace(outputOptions.DeviceName))
            {
                var capabilities = GetCapabilities();

                if (capabilities.Any(c => c.Value.ProductName == outputOptions.DeviceName))
                {
                    var deviceNumber = capabilities
                        .First(c => c.Value.ProductName == outputOptions.DeviceName).Key;

                    Set(deviceNumber);
                }
                else
                {
                    throw new DeviceNotAvailableException(
                        deviceName: outputOptions.DeviceName);
                }
            }
            else
            {
                throw new DeviceNotDefinedException();
            }
        }

        public void Lower()
        {
            if (!isReducedBackground)
            {
                var volume = output.Volume * (float)outputOptions.VolumePortionBackground;

                isReducedBackground = true;

                backgroundFader.Fade(
                    volumeTarget: volume,
                    milliSeconds: settingsService.Contents.Options.FadeOnBackgroundMilliseconds);
            }
        }

        public void Raise()
        {
            isReducedBackground = false;

            backgroundFader.Fade(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: settingsService.Contents.Options.FadeOnBackgroundMilliseconds);
        }

        public void Set(int deviceNumber, int sampleRate = Defaults.SampleRate)
        {
            var capabilities = GetCapabilities();

            if (capabilities.ContainsKey(deviceNumber))
            {
                CloseOutput();

                Format = WaveFormat.CreateIeeeFloatWaveFormat(
                    sampleRate: sampleRate,
                    channels: capabilities[deviceNumber].Channels);

                backgroundMixer = new ContinuousMixingProvider(Format);

                backgroundFader = new FadedControlProvider(
                    sourceProvider: backgroundMixer,
                    volume: Constants.VolumeMax);

                outputMixer = new ContinuousMixingProvider(Format);

                outputMixer.Add(backgroundFader);

                var waveProvider = new NAudio.Wave.SampleProviders.SampleToWaveProvider(outputMixer);

                output = new WaveOut
                {
                    DeviceNumber = deviceNumber,
                    Volume = outputOptions.Volume,
                };

                outputOptions.DeviceName = capabilities[deviceNumber].ProductName;

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

        private Output GetOptions(OutputType type)
        {
            if (!settingsService.Contents.Outputs.Any(o => o.Type == type))
            {
                var outputOptions = new Output
                {
                    Type = type,
                    Volume = Constants.VolumeMax,
                };

                settingsService.Contents.Outputs.Add(outputOptions);
            }

            var result = settingsService.Contents.Outputs
                .Single(s => s.Type == type);

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