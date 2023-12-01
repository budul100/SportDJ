using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons;
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
        private readonly Dictionary<ISignalService, FadingSampleProvider> fadingProviders = new();
        private readonly ISettingsService<Session> settingsService;
        private readonly HashSet<ISignalService> signalServices = new();

        private bool isDisposed;
        private MixingSampleProvider mixingProvider;
        private FadingSampleProvider outputProvider;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator,
            ISettingsService<Session> settingsService)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SignalStartedEvent>().Subscribe(
                action: s => FadeToSignal(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SignalUpdatedEvent>().Subscribe(
                action: s => SendUpdate(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SignalStoppedEvent>().Subscribe(
                action: _ => CheckState(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SignalEndedEvent>().Subscribe(
                action: s => RemoveSignal(s),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsLoop => SignalService?.IsLoop ?? false;

        public bool IsMultiSignalPlayer { get; private set; }

        public string Name { get; private set; }

        public ISampleProvider OutputProvider => outputProvider;

        public TimeSpan Position => SignalService?.Position ?? TimeSpan.Zero;

        public TimeSpan Remaining => SignalService?.Remaining ?? TimeSpan.Zero;

        public ISignalService SignalService { get; private set; }

        public Track Track => SignalService?.Track;

        public float Volume
        {
            get
            {
                return outputProvider?.Volume ?? default;
            }
            set
            {
                if (outputProvider != default)
                {
                    outputProvider.Fade(value);
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public ISignalService Add(Track track)
        {
            if (track == default)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var result = containerProvider.Resolve<ISignalService>();

            result.Initialize(
                waveFormat: outputProvider.WaveFormat,
                track: track);

            if (result.Output != default)
            {
                signalServices.Add(result);

                ISampleProvider signalProvider;

                if (IsMultiSignalPlayer)
                {
                    signalProvider = result.Output;
                }
                else
                {
                    var fadingProvider = new FadingSampleProvider(result.Output);

                    fadingProviders.Add(
                        key: result,
                        value: fadingProvider);

                    fadingProvider.SignalFadedEvent += OnSignalFaded;

                    signalProvider = fadingProvider;
                }

                mixingProvider.Add(signalProvider);
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);
            GC.SuppressFinalize(this);
        }

        public void Initialize(WaveFormat waveFormat, string name, bool isMultiSignalPlayer)
        {
            Name = name;
            IsMultiSignalPlayer = isMultiSignalPlayer;

            mixingProvider = new MixingSampleProvider(
                waveFormat: waveFormat);

            outputProvider = new FadingSampleProvider(
                sourceProvider: mixingProvider);

            outputProvider.SignalStartedEvent += OnSignalStarted;
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

                    mixingProvider.Dispose();

                    signalServices.OnEach(s => s.Dispose());
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CheckState()
        {
            if (!signalServices.Any(s => s.IsPlaying))
            {
                eventAggregator.GetEvent<MixerStoppedEvent>().Publish(this);
            }
        }

        private void FadeToSignal(ISignalService signalService)
        {
            if (signalService != default)
            {
                SignalService = signalService;

                if (fadingProviders.ContainsKey(signalService))
                {
                    var otherSignalServices = signalServices
                        .Where(s => s != SignalService
                            && s.IsPlaying).ToArray();

                    var fadeInMilliseconds = settingsService.Settings.Options.FadeDurationMixIn > 0
                        && otherSignalServices.Any(s => s.IsPlaying)
                        ? settingsService.Settings.Options.FadeDurationMixIn
                        : 0;

                    var fadeOutMilliSeconds = settingsService.Settings.Options.FadeDurationMixOut > 0
                        && otherSignalServices.Any(s => s.IsPlaying)
                        ? settingsService.Settings.Options.FadeDurationMixOut
                        : 0;

                    fadingProviders[SignalService].Fade(
                        volume: Constants.VolumeMax,
                        milliSeconds: fadeInMilliseconds);

                    otherSignalServices.OnEach(s => fadingProviders[s].Fade(
                        volume: Constants.VolumeMin,
                        milliSeconds: fadeOutMilliSeconds));
                }
            }
        }

        private void OnSignalFaded(object sender, EventArgs e)
        {
            if (sender is ISignalService signalService
                && fadingProviders.ContainsKey(signalService)
                && fadingProviders[signalService].IsSilent)
            {
                signalService.Dispose();
            }
        }

        private void OnSignalStarted(object sender, EventArgs e)
        {
            if (signalServices.Count(s => s.IsStarted) == 1)
            {
                eventAggregator.GetEvent<MixerStartedEvent>().Publish(this);
            }
        }

        private void RemoveSignal(ISignalService signalService)
        {
            if (signalServices.Contains(signalService))
            {
                if (SignalService == signalService)
                {
                    SignalService = default;
                }

                if (fadingProviders.ContainsKey(signalService))
                {
                    fadingProviders[SignalService].SignalFadedEvent -= OnSignalFaded;

                    mixingProvider.Remove(fadingProviders[SignalService]);
                    fadingProviders.Remove(signalService);
                }
                else if (signalService.Output != default)
                {
                    mixingProvider.Remove(signalService.Output);
                }

                signalServices.Remove(signalService);
            }
        }

        private void SendUpdate(ISignalService signalService)
        {
            if (!IsMultiSignalPlayer
                && signalService == SignalService)
            {
                eventAggregator.GetEvent<MixerUpdatedEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}