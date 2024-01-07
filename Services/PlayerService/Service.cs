using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.PlayerService
{
    public class Service
        : IPlayerService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly Dictionary<ISignalService, FadedControlProvider> fadingProviders = new();
        private readonly IStorageService<Set> settingsService;
        private readonly HashSet<ISignalService> signalServices = new();

        private bool isDisposed;
        private bool isMultiSignalPlayer;
        private ContinuousMixingProvider mixingProvider;
        private FadedControlProvider outputProvider;
        private ISignalService signalService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IStorageService<Set> settingsService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SignalStartedEvent>().Subscribe(
                action: s => OnSignalStarted(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: _ => OnSignalUpdated(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == signalService);

            eventAggregator.GetEvent<SignalStoppedEvent>().Subscribe(
                action: _ => OnSignalStopped(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));

            eventAggregator.GetEvent<SignalEndedEvent>().Subscribe(
                action: s => OnSignalEnded(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsLoop => signalService?.IsLoop == true;

        public bool IsPlaying { get; private set; }

        public ISampleProvider Output => outputProvider;

        public TimeSpan Position => signalService?.Position ?? TimeSpan.Zero;

        public TimeSpan Remaining => signalService?.Remaining ?? TimeSpan.Zero;

        public Track Track => signalService?.Track;

        public PlayerType Type { get; private set; }

        public float Volume
        {
            get
            {
                return outputProvider?.Volume ?? default;
            }
            set
            {
                if (outputProvider != default
                    && value != outputProvider.Volume)
                {
                    outputProvider.Fade(value);
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public ISignalService Add(Track track)
        {
            var result = default(ISignalService);

            if (track != default)
            {
                result = containerProvider.Resolve<ISignalService>();

                result.Initialize(
                    track: track,
                    waveFormat: outputProvider.WaveFormat);

                if (result.Output != default)
                {
                    signalServices.Add(result);

                    ISampleProvider signalProvider;

                    if (isMultiSignalPlayer)
                    {
                        signalProvider = result.Output;
                    }
                    else
                    {
                        var fadingProvider = new FadedControlProvider(result.Output);

                        fadingProvider.FadingEndedEvent += OnFadingEnded;

                        fadingProviders.Add(
                            key: result,
                            value: fadingProvider);

                        signalProvider = fadingProvider;
                    }

                    mixingProvider.Add(signalProvider);
                }
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(PlayerType type, WaveFormat waveFormat, bool isMultiSignalPlayer)
        {
            Type = type;

            this.isMultiSignalPlayer = isMultiSignalPlayer;

            mixingProvider = new ContinuousMixingProvider(waveFormat);

            outputProvider = new FadedControlProvider(
                sourceProvider: mixingProvider,
                volume: Constants.VolumeMax);
        }

        public void Pause()
        {
            signalServices.OnEach(s => s.Pause());
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    Pause();

                    eventAggregator.GetEvent<PlayerEndedEvent>().Publish(this);

                    mixingProvider.Dispose();

                    signalServices.OnEach(s => s.Dispose());

                    outputProvider.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnFadingEnded(object sender, EventArgs e)
        {
            if (sender is FadedControlProvider fadingProvider
                && fadingProvider.IsSilent)
            {
                var signalService = fadingProviders
                    .SingleOrDefault(p => p.Value == fadingProvider).Key;

                signalService.Stop();
            }
        }

        private void OnSignalEnded(ISignalService signalService)
        {
            if (this.signalService == signalService)
            {
                this.signalService = default;
            }

            if (fadingProviders.ContainsKey(signalService))
            {
                fadingProviders[signalService].FadingEndedEvent -= OnFadingEnded;

                mixingProvider.Remove(fadingProviders[signalService]);
                fadingProviders.Remove(signalService);
            }
            else if (signalService.Output != default)
            {
                mixingProvider.Remove(signalService.Output);
            }

            signalServices.Remove(signalService);
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            this.signalService = signalService;

            if (fadingProviders.ContainsKey(signalService))
            {
                var otherSignalServices = signalServices
                    .Where(s => s != signalService
                        && s.State == StateType.Play).ToArray();

                var fadeInMilliseconds = settingsService.Contents.Options.FadeOnMixInMilliseconds > 0 && otherSignalServices.Any()
                    ? settingsService.Contents.Options.FadeOnMixInMilliseconds
                    : 0;

                var fadeOutMilliSeconds = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0 && otherSignalServices.Any()
                    ? settingsService.Contents.Options.FadeOnMixOutMilliseconds
                    : 0;

                fadingProviders[signalService].Fade(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeInMilliseconds);

                otherSignalServices.OnEach(s => fadingProviders[s].Fade(
                    volumeTarget: Constants.VolumeMin,
                    milliSeconds: fadeOutMilliSeconds));
            }

            if (!IsPlaying)
            {
                IsPlaying = true;

                eventAggregator.GetEvent<PlayerStartedEvent>().Publish(this);
            }
        }

        private void OnSignalStopped()
        {
            if (IsPlaying
                && !signalServices.Any(s => s.State == StateType.Play))
            {
                IsPlaying = false;

                eventAggregator.GetEvent<PlayerStoppedEvent>().Publish(this);
            }
        }

        private void OnSignalUpdated()
        {
            if (!isMultiSignalPlayer)
            {
                eventAggregator.GetEvent<PlayerUpdatedEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}