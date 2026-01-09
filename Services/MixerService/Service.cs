using System;
using System.Collections.Generic;
using System.Linq;
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
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.MixerService
{
    public class Service
        : IMixerService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly Dictionary<ISignalService, DynamicFadingProvider> fadingProviders = [];
        private readonly object lockFadingProviders = new();
        private readonly object lockSignalServices = new();
        private readonly IStorageService<Set> settingsService;
        private readonly HashSet<ISignalService> signalServices = [];

        private bool isDisposed;
        private ContinuousMixingProvider mixingProvider;
        private DynamicFadingProvider outputProvider;
        private bool signalsCanOverlap;
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
                action: OnSignalStarted,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: signalServices.Contains);

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
                action: OnSignalEnded,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: signalServices.Contains);
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan? Duration => signalService?.Duration;

        public bool IsEnding => signalService?.IsEnding == true
            && signalService?.IsLoop == false;

        public bool IsInitialized => mixingProvider != default;

        public bool IsLoop => signalService?.IsLoop == true;

        public bool IsPlaying { get; private set; }

        public ISampleProvider Output => outputProvider;

        public TimeSpan? Position => signalService?.Position;

        public double? Progress => signalService?.Progress;

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

        public ISignalService Add(Track track, bool isLoop)
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
                    result.IsLoop = isLoop;

                    lock (lockSignalServices)
                    {
                        signalServices.Add(result);
                    }

                    ISampleProvider signalProvider;

                    if (signalsCanOverlap)
                    {
                        signalProvider = result.Output;
                    }
                    else
                    {
                        var fadingProvider = new DynamicFadingProvider(result.Output);

                        fadingProvider.FadingEndedEvent += OnFadingEnded;

                        lock (lockFadingProviders)
                        {
                            fadingProviders.Add(
                                key: result,
                                value: fadingProvider);
                        }

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

        public void End()
        {
            signalServices.OnEach(s => s?.End());
        }

        public void Initialize(MixerType type, WaveFormat waveFormat, bool signalsCanOverlap)
        {
            Type = type;

            this.signalsCanOverlap = signalsCanOverlap;

            if (mixingProvider?.WaveFormat != waveFormat)
            {
                mixingProvider = new ContinuousMixingProvider(
                    waveFormat: waveFormat);

                outputProvider = new DynamicFadingProvider(
                    sourceProvider: mixingProvider,
                    volume: Constants.VolumeMax);

                eventAggregator.GetEvent<MixerInitializedEvent>().Publish(this);
            }
        }

        public void Pause()
        {
            var useFilterEffect = Type == MixerType.Background;

            signalServices.OnEach(s => s?.Pause(useFilterEffect));
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    End();

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
            var isPlaying = signalServices.Any(s => s.IsPlaying);

            if (IsPlaying && !isPlaying)
            {
                IsPlaying = false;

                eventAggregator.GetEvent<MixerStoppedEvent>().Publish(this);
            }
        }

        private void OnFadingEnded(object sender, EventArgs e)
        {
            if (sender is DynamicFadingProvider fadingProvider
                && fadingProvider.IsSilent)
            {
                var signalService = fadingProviders
                    .SingleOrDefault(p => p.Value == fadingProvider).Key;

                signalService.End();
            }
        }

        private void OnSignalEnded(ISignalService signalService)
        {
            if (this.signalService == signalService)
            {
                this.signalService = default;
            }

            EndMixer();

            var relevants = default(ISignalService[]);

            lock (lockSignalServices)
            {
                relevants = signalServices
                    .Where(s => s == signalService || (s.IsSilent && s.IsOverNextThreshold)).ToArray();

                signalServices.ExceptWith(relevants);
            }

            relevants?.OnEach(RemoveSignal);
        }

        private void OnSignalStarted(ISignalService signalService)
        {
            this.signalService = signalService;

            if (fadingProviders.TryGetValue(
                key: signalService,
                value: out var fadingProvider))
            {
                var playingServices = signalServices
                    .Where(s => s != signalService
                        && s?.IsPlaying == true).ToArray();

                var pausingServices = signalServices
                    .Where(s => s != signalService
                        && s?.State == StateType.Pause).ToArray();

                var fadeInMilliseconds = settingsService.Contents.Options.FadeOnMixInMilliseconds > 0 && playingServices.Length != 0
                    ? settingsService.Contents.Options.FadeOnMixInMilliseconds
                    : 0;

                var fadeOutMilliSeconds = settingsService.Contents.Options.FadeOnMixOutMilliseconds > 0 && playingServices.Length != 0
                    ? settingsService.Contents.Options.FadeOnMixOutMilliseconds
                    : 0;

                fadingProvider.FadeIn(
                    milliSeconds: fadeInMilliseconds);

                pausingServices.OnEach(s => s.End());

                playingServices.OnEach(s => fadingProviders[s].FadeOut(
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
            if (!signalsCanOverlap)
            {
                eventAggregator.GetEvent<MixerUpdatedEvent>().Publish(this);
            }
        }

        private void RemoveSignal(ISignalService signalService)
        {
            if (fadingProviders.TryGetValue(
                signalService,
                out var fadingProvider))
            {
                fadingProvider.FadingEndedEvent -= OnFadingEnded;

                lock (lockFadingProviders)
                {
                    fadingProviders.Remove(signalService);
                }

                mixingProvider.Remove(fadingProvider);
            }
            else if (signalService.Output != default)
            {
                mixingProvider.Remove(signalService.Output);
            }
        }

        #endregion Private Methods
    }
}