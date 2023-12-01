using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Prism.Events;
using Score2Stream.Commons;
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
        private readonly ISettingsService<Session> settingsService;

        private bool isDisposed;
        private FadingSampleProvider outputProvider;
        private PlayingFileProvider playingProvider;
        private TimeSpan selectNextThreshold;
        private bool selectNextThresholdExceeded;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, ISettingsService<Session> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;
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

        public bool IsPlaying { get; private set; }

        public bool IsStarted { get; private set; }

        public ISampleProvider Output => outputProvider;

        public TimeSpan Position => playingProvider?.Current ?? TimeSpan.Zero;

        public TimeSpan Remaining => playingProvider?.Remaining ?? TimeSpan.Zero;

        public Track Track { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(WaveFormat waveFormat, Track track, TimeSpan? begin = default,
            TimeSpan? length = default)
        {
            if (track == default)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (File.Exists(track.Path))
            {
                Track = track;

                var reader = new AudioFileReader(Track.Path);

                begin ??= Track.Start;
                length ??= Track.Length;

                playingProvider = new PlayingFileProvider(
                    reader: reader,
                    track: Track,
                    begin: begin,
                    length: length);

                var duration = (int)(settingsService.Settings.Options.SelectNextPortion * playingProvider.Duration.Ticks);
                selectNextThreshold = new TimeSpan(duration);

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

                outputProvider = new FadingSampleProvider(
                    sourceProvider: resampleProvider,
                    volume: Constants.VolumeMin);

                outputProvider.SignalStartedEvent += OnSignalStarted;
                outputProvider.SignalFadedEvent += OnSignalFaded;
            }
        }

        public void Pause()
        {
            if (playingProvider != default
                && IsPlaying)
            {
                var milliSeconds = settingsService.Settings.Options.FadeDurationPause;

                outputProvider.Fade(
                    volume: Constants.VolumeMin,
                    milliSeconds: milliSeconds);

                IsPlaying = false;
            }
        }

        public void Start()
        {
            if (playingProvider != default)
            {
                playingProvider.Start();

                var milliSeconds = IsStarted
                    ? settingsService.Settings.Options.FadeDurationPause
                    : 0;

                outputProvider.Fade(
                    volume: Constants.VolumeMax,
                    milliSeconds: milliSeconds);

                IsStarted = true;
                IsPlaying = true;
            }
        }

        public void Stop()
        {
            if (playingProvider != default
                && IsPlaying)
            {
                playingProvider.Pause();

                IsPlaying = false;

                eventAggregator.GetEvent<SignalStoppedEvent>().Publish(this);
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

                    outputProvider.SignalStartedEvent -= OnSignalStarted;
                    outputProvider.SignalFadedEvent -= OnSignalFaded;

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

        private void OnSignalEnded(object sender, EventArgs e)
        {
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
            eventAggregator.GetEvent<SignalStartedEvent>().Publish(this);
        }

        private void OnSignalUpdated(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<SignalUpdatedEvent>().Publish(this);

            if (!selectNextThresholdExceeded
                && Position.Ticks > selectNextThreshold.Ticks)
            {
                selectNextThresholdExceeded = true;

                eventAggregator.GetEvent<SignalNextThresholdExceededEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}