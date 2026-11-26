using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Shareds.SampleProviders.Core;

namespace SportDJ.Services.MixerService;

public class Service
    : IMixerService
{
    #region Private Fields

    private readonly IContainerProvider containerProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly Dictionary<ISignalService, FadingProvider> fadingProviders = [];
    private readonly Lock lockFadingProviders = new();
    private readonly Lock lockSignalServices = new();
    private readonly IStorageService<Set> settingsService;
    private readonly HashSet<ISignalService> signalServices = [];

    private bool isDisposed;
    private MixingProvider mixingProvider;
    private FadingProvider outputProvider;
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
            filter: IsOwnSignal);

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
            filter: IsOwnSignal);
    }

    #endregion Public Constructors

    #region Public Properties

    public TimeSpan? Duration => signalService?.Duration;

    public bool IsEnding => signalService?.IsEnding == true
        && signalService?.IsLoop == false;

    public bool IsInitialized => mixingProvider != default;

    public bool IsLoop => signalService?.IsLoop == true;

    public bool IsLoopReady => signalService?.IsLoopReady == true;

    public bool IsPlaying { get; private set; }

    public ISampleProvider Output => outputProvider;

    public TimeSpan? Position => signalService?.Current;

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

    public void ActivateLoop()
    {
        signalService?.ActivateLoop();
    }

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
                    var fadingProvider = new FadingProvider(result.Output);

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

    public void DeactivateLoop()
    {
        signalService?.DeactivateLoop();
    }

    public void Dispose()
    {
        Dispose(
            isDisposing: true);

        GC.SuppressFinalize(this);
    }

    public void End()
    {
        GetSignalServices().OnEach(s => s?.End());
    }

    public void Initialize(MixerType type, WaveFormat waveFormat, bool signalsCanOverlap)
    {
        Type = type;

        this.signalsCanOverlap = signalsCanOverlap;

        if (mixingProvider?.WaveFormat != waveFormat)
        {
            mixingProvider = new MixingProvider(
                waveFormat: waveFormat);

            outputProvider = new FadingProvider(
                sourceProvider: mixingProvider,
                volume: Constants.VolumeMax);

            eventAggregator.GetEvent<MixerInitializedEvent>().Publish(this);
        }
    }

    public void Pause()
    {
        var useFilterEffect = Type == MixerType.Base;

        GetSignalServices().OnEach(s => s?.Pause(useFilterEffect));
    }

    public void Remove(ISignalService signalService)
    {
        if (signalService == default)
        {
            return;
        }

        var isKnown = false;

        lock (lockSignalServices)
        {
            isKnown = signalServices.Remove(signalService);
        }

        // The provider has to leave the mix before the signal is disposed, otherwise the audio thread keeps reading it
        if (isKnown)
        {
            RemoveSignal(signalService);
        }

        if (this.signalService == signalService)
        {
            this.signalService = default;
        }

        EndMixer();
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

                GetSignalServices().OnEach(s => s.Dispose());

                outputProvider.Dispose();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void EndMixer()
    {
        var isPlaying = GetSignalServices().Any(s => s.IsPlaying);

        if (IsPlaying && !isPlaying)
        {
            IsPlaying = false;

            eventAggregator.GetEvent<MixerStoppedEvent>().Publish(this);
        }
    }

    private ISignalService[] GetSignalServices()
    {
        lock (lockSignalServices)
        {
            return [.. signalServices];
        }
    }

    // Used as event filter, which runs on the thread of the publisher
    private bool IsOwnSignal(ISignalService signalService)
    {
        lock (lockSignalServices)
        {
            return signalServices.Contains(signalService);
        }
    }

    private void OnFadingEnded(object sender, EventArgs e)
    {
        if (sender is FadingProvider fadingProvider
            && fadingProvider.IsSilent)
        {
            var signalService = default(ISignalService);

            lock (lockFadingProviders)
            {
                signalService = fadingProviders
                    .FirstOrDefault(p => p.Value == fadingProvider).Key;
            }

            // The signal might have been removed in the meantime
            signalService?.End();
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
                .Where(s => s == signalService
                    || (s.IsSilent && s.IsOverNextThreshold)).ToArray();

            signalServices.ExceptWith(relevants);
        }

        relevants?.OnEach(RemoveSignal);
    }

    private void OnSignalStarted(ISignalService signalService)
    {
        this.signalService = signalService;

        if (TryGetFadingProvider(
            signalService: signalService,
            fadingProvider: out var fadingProvider))
        {
            var currents = GetSignalServices();

            var playingServices = currents
                .Where(s => s != signalService
                    && s?.IsPlaying == true).ToArray();

            var pausingServices = currents
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

            // A signal might have been removed in the meantime
            playingServices.OnEach(s =>
            {
                if (TryGetFadingProvider(s, out var playingProvider))
                {
                    playingProvider.FadeOut(
                        milliSeconds: fadeOutMilliSeconds);
                }
            });
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
        var fadingProvider = default(FadingProvider);
        var hasFading = false;

        lock (lockFadingProviders)
        {
            hasFading = fadingProviders.Remove(
                key: signalService,
                value: out fadingProvider);
        }

        if (hasFading)
        {
            fadingProvider.FadingEndedEvent -= OnFadingEnded;

            mixingProvider.Remove(fadingProvider);
        }
        else if (signalService.Output != default)
        {
            mixingProvider.Remove(signalService.Output);
        }
    }

    private bool TryGetFadingProvider(ISignalService signalService, out FadingProvider fadingProvider)
    {
        lock (lockFadingProviders)
        {
            return fadingProviders.TryGetValue(
                key: signalService,
                value: out fadingProvider);
        }
    }

    #endregion Private Methods
}
