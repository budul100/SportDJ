using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Providers;
using System;
using System.IO;

namespace SportDJ.Services.SignalService
{
    public class Service
        : ISignalService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Set> settingsService;
        private readonly ITrackService trackService;

        private bool isDisposed;
        private TimeSpan nextThreshold;
        private FadedControlProvider outputProvider;
        private PlayingFileProvider playingProvider;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
            ITrackService trackService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
            this.trackService = trackService;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsLoop
        {
            get
            {
                return playingProvider?.IsLoop ?? default;
            }
            set
            {
                if (playingProvider != default)
                {
                    playingProvider.IsLoop = value;
                }
            }
        }

        public bool IsNextThresholdExceeded { get; private set; }

        public bool IsSilent => outputProvider?.IsSilent ?? true;

        public ISampleProvider Output => outputProvider;

        public TimeSpan Position => playingProvider?.Current ?? TimeSpan.Zero;

        public TimeSpan Remaining => playingProvider?.Remaining ?? TimeSpan.Zero;

        public StateType State { get; private set; }

        public Track Track { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(Track track, WaveFormat waveFormat, TimeSpan? begin = default,
            TimeSpan? length = default)
        {
            if (track == default)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (File.Exists(track.Path))
            {
                Track = track;

                var sourceProvider = new AudioFileReader(Track.Path);

                begin ??= Track.Start;
                length ??= Track.Length;

                playingProvider = new PlayingFileProvider(
                    sourceProvider: sourceProvider,
                    track: Track,
                    begin: begin,
                    length: length);

                nextThreshold = begin.Value.Add(
                    ts: TimeSpan.FromSeconds(settingsService.Contents.Options.SelectNextSeconds));

                playingProvider.SignalUpdatedEvent += OnSignalUpdated;
                playingProvider.SignalEndedEvent += OnSignalEnded;

                var channelProvider = default(ISampleProvider);

                if (playingProvider.WaveFormat.Channels == waveFormat.Channels)
                {
                    channelProvider = playingProvider;
                }
                else
                {
                    channelProvider = playingProvider.WaveFormat.Channels == 1
                        ? new MonoToStereoSampleProvider(playingProvider)
                        : new StereoToMonoSampleProvider(playingProvider);
                }

                var resampleProvider = default(ISampleProvider);

                if (channelProvider.WaveFormat.SampleRate == waveFormat.SampleRate)
                {
                    resampleProvider = channelProvider;
                }
                else
                {
                    resampleProvider = new WdlResamplingSampleProvider(
                        source: channelProvider,
                        newSampleRate: waveFormat.SampleRate);
                }

                outputProvider = new FadedControlProvider(resampleProvider);

                outputProvider.FadingStartedEvent += OnSignalStarted;
                outputProvider.FadingEndedEvent += OnSignalFaded;
            }
        }

        public void Pause()
        {
            if (playingProvider != default
                && State == StateType.Play)
            {
                var milliSeconds = settingsService.Contents.Options.FadeOnPauseMilliseconds;

                State = StateType.Pause;

                outputProvider.Fade(
                    volumeTarget: Constants.VolumeMin,
                    milliSeconds: milliSeconds);
            }
        }

        public void Start()
        {
            if (playingProvider != default)
            {
                var milliSeconds = State == StateType.Play
                    ? settingsService.Contents.Options.FadeOnPauseMilliseconds
                    : 0;

                playingProvider.Start();

                State = StateType.Play;

                outputProvider.Fade(
                    volumeTarget: Constants.VolumeMax,
                    milliSeconds: milliSeconds);
            }
        }

        public void Stop()
        {
            if (playingProvider != default)
            {
                playingProvider.Pause();

                EndSignal();
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing
                    && playingProvider != default)
                {
                    playingProvider.Stop();

                    eventAggregator.GetEvent<SignalEndedEvent>().Publish(this);

                    outputProvider.FadingStartedEvent -= OnSignalStarted;
                    outputProvider.FadingEndedEvent -= OnSignalFaded;

                    outputProvider.Dispose();

                    playingProvider.SignalUpdatedEvent -= OnSignalUpdated;
                    playingProvider.SignalEndedEvent -= OnSignalEnded;

                    playingProvider.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void EndSignal()
        {
            if (State != StateType.Pause)
            {
                State = StateType.None;
            }

            trackService.End(Track);

            eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);
        }

        private void OnSignalEnded(object sender, EventArgs e)
        {
            EndSignal();

            Dispose();
        }

        private void OnSignalFaded(object sender, EventArgs e)
        {
            if (outputProvider?.IsSilent == true)
            {
                Stop();
            }
        }

        private void OnSignalStarted(object sender, EventArgs e)
        {
            trackService.Start(Track);

            eventAggregator.GetEvent<SignalStartedEvent>().Publish(this);
        }

        private void OnSignalUpdated(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);

            if (!IsNextThresholdExceeded
                && Position.Ticks > nextThreshold.Ticks)
            {
                IsNextThresholdExceeded = true;

                eventAggregator.GetEvent<SignalNextEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}