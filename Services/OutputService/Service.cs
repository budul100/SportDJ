using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Exceptions;
using SportDJ.Commons.Extensions;
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

        private readonly List<IPlayerService> backgroundPlayers = new List<IPlayerService>();
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly List<IPlayerService> foregroundPlayers = new List<IPlayerService>();
        private readonly IStorageService<Set> settingsService;

        private FadedControlProvider backgroundFader;
        private ContinuousMixingProvider backgroundMixer;
        private bool isDisposed;
        private bool isLowered;
        private WaveOut output;
        private ContinuousMixingProvider outputMixer;
        private Output outputOptions;
        private IVolumeService volumeService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<PlayerStartedEvent>().Subscribe(
                action: p => OnPlayerStarted(p),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<PlayerStoppedEvent>().Subscribe(
                action: p => OnPlayerStopped(p),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public string DeviceName => outputOptions?.DeviceName;

        public WaveFormat Format { get; private set; }

        public bool IsPlaying { get; private set; }

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

        public double VolumePortionApps
        {
            get
            {
                return outputOptions.VolumePortionApps;
            }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != outputOptions.VolumePortionApps)
                {
                    outputOptions.VolumePortionApps = value;

                    if (isLowered)
                    {
                        Lower();
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

                    if (isLowered)
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

                foregroundPlayers.Add(playerService);
            }
            else
            {
                backgroundMixer.Add(playerService.Output);
            }

            backgroundPlayers.Add(playerService);
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

            volumeService = containerProvider.Resolve<IVolumeService>(
                name: type.ToString());

            var deviceNumber = GetDeviceNumber();

            Set(
                deviceNumber: deviceNumber);
        }

        public void Lower()
        {
            if (!isLowered)
            {
                isLowered = true;

                var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

                var volumeBackground = output.Volume * (float)outputOptions.VolumePortionBackground;

                backgroundFader.Fade(
                    volumeTarget: volumeBackground,
                    milliSeconds: fadeMilliseconds);

                var volumeApps = output.Volume * (float)outputOptions.VolumePortionApps;

                volumeService.Reduce(
                    volumeTarget: volumeApps,
                    milliSeconds: fadeMilliseconds);
            }
        }

        public void Pause()
        {
            foregroundPlayers.OnEach(p => p.Pause());
            backgroundPlayers.OnEach(p => p.Pause());

            var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

            volumeService.Reduce(
                volumeTarget: Constants.VolumeMin,
                milliSeconds: fadeMilliseconds);
        }

        public void Raise()
        {
            var fadeMilliseconds = settingsService.Contents.Options.FadeOnLowerMilliseconds;

            isLowered = false;

            backgroundFader.Fade(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: fadeMilliseconds);

            volumeService.Raise(
                volumeTarget: Constants.VolumeMax,
                milliSeconds: fadeMilliseconds);
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

                volumeService.Set(
                    deviceNumber: deviceNumber);
            }
            else
            {
                throw new NoDeviceAvailableException(
                    deviceNumber: deviceNumber);
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

                volumeService.Deactivate();
                volumeService.Dispose();

                backgroundPlayers.Clear();
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

        private int GetDeviceNumber()
        {
            var result = default(int?);

            var capabilities = GetCapabilities();

            if (string.IsNullOrWhiteSpace(outputOptions.DeviceName))
            {
                result = capabilities
                    .FirstOrDefault(c => c.Value.ProductName == outputOptions.DeviceName).Key;
            }

            if (!result.HasValue)
            {
                result = capabilities
                    .FirstOrDefault().Key;
            }

            if (!result.HasValue)
            {
                throw new NoDeviceAvailableException();
            }

            return result.Value;
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

        private void OnPlayerStarted(IPlayerService playerService)
        {
            if (foregroundPlayers.Contains(playerService)
                || backgroundPlayers.Contains(playerService))
            {
                if (!IsPlaying)
                {
                    IsPlaying = true;

                    eventAggregator.GetEvent<OutputStartedEvent>().Publish(this);
                }

                if (foregroundPlayers.Contains(playerService))
                {
                    Lower();
                }
            }
        }

        private void OnPlayerStopped(IPlayerService playerService)
        {
            if (foregroundPlayers.Contains(playerService)
                || backgroundPlayers.Contains(playerService))
            {
                if (foregroundPlayers.Contains(playerService))
                {
                    Raise();
                }

                if (!foregroundPlayers.Any(p => p.IsPlaying)
                    && !backgroundPlayers.Any(p => p.IsPlaying))
                {
                    IsPlaying = false;

                    eventAggregator.GetEvent<OutputStoppedEvent>().Publish(this);
                }
            }
        }

        #endregion Private Methods
    }
}