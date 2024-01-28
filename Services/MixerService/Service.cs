using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.MixerService
{
    public class Service
        : IMixerService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly Dictionary<ISignalService, FadedControlProvider> fadingProviders = new();
        private readonly IStorageService<Set> settingsService;
        private readonly HashSet<ISignalService> signalServices = new();

        private bool isDisposed;
        private ContinuousMixingProvider mixingProvider;
        private FadedControlProvider outputProvider;
        private bool playsMultipleSignals;
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
                filter: s => s == signalService);

            eventAggregator.GetEvent<SignalEndedEvent>().Subscribe(
                action: s => OnSignalEnded(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => signalServices.Contains(s));
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan? Duration => signalService?.Duration;

        public bool IsEnding => signalService?.IsEnding == true;

        public bool IsLoop => signalService?.IsLoop == true;

        public bool IsPlaying { get; private set; }

        public ISampleProvider Output => outputProvider;

        public TimeSpan? Position => signalService?.Position;

        public TimeSpan? Remaining => signalService?.Remaining;

        public Track Track => signalService?.Track;

        public MixerType Type { get; private set; }

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

            if (track?.IsSilent == false)
            {
                result = containerProvider.Resolve<ISignalService>();

                result.Initialize(
                    track: track,
                    waveFormat: outputProvider.WaveFormat);

                if (result.Output != default)
                {
                    lock (mixingProvider)
                    {
                        signalServices.Add(result);

                        ISampleProvider signalProvider;

                        if (playsMultipleSignals)
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
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(MixerType type, WaveFormat waveFormat, bool playsMultipleSignals)
        {
            Type = type;

            this.playsMultipleSignals = playsMultipleSignals;

            mixingProvider = new ContinuousMixingProvider(waveFormat);

            outputProvider = new FadedControlProvider(
                sourceProvider: mixingProvider,
                volume: Constants.VolumeMax);
        }

        public void Pause()
        {
            signalServices.OnEach(s => s?.Pause());
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

                    eventAggregator.GetEvent<MixerEndedEvent>().Publish(this);

                    mixingProvider.Dispose();

                    signalServices.OnEach(s => s.Dispose());

                    outputProvider.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void EndMixer()
        {
            if (IsPlaying
                && !signalServices.Any(s => s.IsPlaying))
            {
                IsPlaying = false;

                eventAggregator.GetEvent<MixerStoppedEvent>().Publish(this);
            }
        }

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

            EndMixer();

            signalServices
                .Where(s => s == signalService || s.IsOverNextThreshold)
                .OnEach(s => RemoveSignal(s));
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            this.signalService = signalService;

            if (fadingProviders.ContainsKey(signalService))
            {
                var playingServices = signalServices
                    .Where(s => s != signalService
                        && s?.IsPlaying == true).ToArray();

                var pausingServices = signalServices
                    .Where(s => s != signalService
                        && s?.State == StateType.Pause).ToArray();

                var fadeInMilliseconds = settingsService.Contents.Options.FadeOnMixInMilliseconds > 0 && playingServices.Any()
                    ? settingsService.Contents.Options.FadeOnMixInMilliseconds
                    : 0;

                var fadeOutMilliSeconds = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0 && playingServices.Any()
                    ? settingsService.Contents.Options.FadeOnMixOutMilliseconds
                    : 0;

                fadingProviders[signalService].Fade(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: fadeInMilliseconds);

                pausingServices.OnEach(s => s.Stop());

                playingServices.OnEach(s => fadingProviders[s].Fade(
                    volumeTarget: Constants.VolumeMin,
                    milliSeconds: fadeOutMilliSeconds));
            }

            if (!IsPlaying)
            {
                IsPlaying = true;

                eventAggregator.GetEvent<MixerStartedEvent>().Publish(this);
            }
        }

        private void OnSignalStopped()
        {
            EndMixer();
        }

        private void OnSignalUpdated()
        {
            if (!playsMultipleSignals)
            {
                eventAggregator.GetEvent<MixerUpdatedEvent>().Publish(this);
            }
        }

        private void RemoveSignal(ISignalService signalService)
        {
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

        #endregion Private Methods
    }
}