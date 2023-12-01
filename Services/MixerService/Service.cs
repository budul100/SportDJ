using NAudio.Wave;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Signal;
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
        private bool isExclusive;
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
                action: s => FadeIn(s),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsLoop => SignalService?.IsLoop ?? false;

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

        public void Dispose()
        {
            Dispose(
                isDisposing: true);
            GC.SuppressFinalize(this);
        }

        public ISignalService Get(Track track)
        {
            if (track == default)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var result = containerProvider.Resolve<ISignalService>();

            result.Initialize(
                waveFormat: outputProvider.WaveFormat,
                track: track);

            signalServices.Add(result);

            ISampleProvider signalProvider;

            if (!isExclusive)
            {
                signalProvider = result.Output;
            }
            else
            {
                var fadingProvider = new FadingSampleProvider(result.Output);

                fadingProviders.Add(
                    key: result,
                    value: fadingProvider);

                signalProvider = fadingProvider;
            }

            mixingProvider.Add(signalProvider);

            return result;
        }

        public void Initialize(string name, bool isExclusive, WaveFormat waveFormat)
        {
            Name = name;

            this.isExclusive = isExclusive;

            mixingProvider = new MixingSampleProvider(
                waveFormat: waveFormat);

            outputProvider = new FadingSampleProvider(
                sourceProvider: mixingProvider);
        }

        public void Pause()
        {
            Execute(s => s.Pause());
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

                    signals.ForEach(p => RemovePlayer(p));
                    mixProvider?.RemoveAllMixerInputs();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void Execute(Action<ISignalService> action, IEnumerable<ISignalService> services = default)
        {
            if (action != default)
            {
                var relevants = services ?? signalServices;

                if (relevants?.Any() == true)
                {
                    foreach (var relevant in relevants)
                    {
                        action(relevant);
                    }
                }
            }
        }

        private void FadeIn(ISignalService signalService)
        {
            if (fadingProviders.ContainsKey(signalService))
            {
                SignalService = signalService;

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

                eventAggregator.GetEvent<MixerStartedEvent>().Publish(this);

                Execute(s => fadingProviders[s].Fade(
                    volume: Constants.VolumeMin,
                    milliSeconds: fadeOutMilliSeconds),
                    services: otherSignalServices);
            }
        }

        private void FadingEnded(object sender, EventArgs e)
        {
            var provider = (FadingSampleProvider)sender;
            if (provider.Volume == 0f)
            {
                var fadedPlayer = GetPlayerFomProvider(provider);
                if (fadedPlayer?.IsStarted == true)
                    fadedPlayer.End();
            }
        }

        private Signal GetPlayerFomProvider(ISampleProvider provider)
        {
            Signal result = null;

            foreach (var f in fadingProviders)
            {
                if (f.Value == provider)
                {
                    result = signals.Find(p => p.ToString() == f.Key);
                    break;
                }
            }

            return result;
        }

        private void PlayingEnded(object sender, EventArgs e)
        {
            RemovePlayer((Signal)sender);
        }

        private void PlayingStarted(object sender, EventArgs e)
        {
            FadeOverPlayers();
        }

        private void PlayingStopped(object sender, EventArgs e)
        {
            SendMixerStopped();
        }

        private void PlayingUpdated(object sender, EventArgs e)
        {
            Updated?.Invoke(this, null);
        }

        private void RemovePlayer(Signal player)
        {
            signals.Remove(player);
            player.Started -= PlayingStarted;
            player.Ended -= PlayingEnded;

            if (fadingProviders == null)
            {
                mixProvider.RemoveMixerInput(player.Output);
            }
            else
            {
                fadingProviders[player.ToString()].Faded -= FadingEnded;
                mixProvider.RemoveMixerInput(fadingProviders[player.ToString()]);

                fadingProviders.Remove(player.ToString());
            }

            if (player == currentSignal) { currentSignal.Updated -= PlayingUpdated; }
        }

        private void SendMixerStopped()
        {
            if (!HasActivePlayers())
                Stopped?.Invoke(this, null);
        }

        private void SetAsCurrentPlayer(Signal player)
        {
            if (currentSignal != null)
                currentSignal.Updated -= PlayingUpdated;

            currentSignal = player;
            Track = tracks[currentSignal.ToString()];

            currentSignal.Updated += PlayingUpdated;
        }

        #endregion Private Methods
    }
}