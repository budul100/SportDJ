using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.SignalService.Providers;
using System;
using System.IO;

namespace SportDJ.Services.SignalService
{
    public class Service
        : ISignalService
    {
        #region Private Fields

        private const float FadingVolunmeDown = 0f;
        private const float FadingVolunmeUp = 1f;

        private readonly IEventAggregator eventAggregator;
        private readonly ISettingsService<Set> settingsService;
        private readonly ITrackService trackService;
        private FadingSampleProvider fadingProvider;

        private bool isDisposed;
        private PlayingFileProvider playingProvider;
        private TimeSpan selectNextThreshold;
        private bool selectNextThresholdExceeded;
        private Track track;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, ITrackService trackService,
            ISettingsService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.trackService = trackService;
            this.settingsService = settingsService;
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan FadeDuration { get; set; }

        public bool IsLoop
        {
            get
            {
                return playingProvider?.IsLoop == true;
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

        public ISampleProvider Output => fadingProvider;

        public TimeSpan Position => playingProvider?.Current
            ?? TimeSpan.Zero;

        public TimeSpan Remaining => playingProvider?.Remaining
            ?? TimeSpan.Zero;

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(this);
        }

        public void End()
        {
            if (playingProvider != default)
            {
                playingProvider.Stop();

                IsPlaying = false;

                playingProvider.Dispose();
            }
        }

        public void Initialize(TimeSpan begin, TimeSpan length)
        {
            if (trackService?.Track != default
                && File.Exists(trackService.Track.Path))
            {
                track = trackService.Track;

                var reader = new AudioFileReader(track.Path);

                playingProvider = new PlayingFileProvider(
                    eventAggregator: eventAggregator,
                    reader: reader,
                    track: track,
                    begin: begin,
                    length: length);

                fadingProvider = new FadingSampleProvider(
                    eventAggregator: eventAggregator,
                    source: playingProvider,
                    track: track,
                    volume: FadingVolunmeUp);

                var duration = (int)(settingsService.Settings.SelectNextPortion * playingProvider.Duration.Ticks);
                this.selectNextThreshold = new TimeSpan(duration);

                eventAggregator.GetEvent<SignalEndedEvent>().Subscribe(
                    action: _ => End(),
                    keepSubscriberReferenceAlive: true);

                eventAggregator.GetEvent<SignalUpdatedEvent>().Subscribe(
                    action: _ => OnSignalUpdated(),
                    keepSubscriberReferenceAlive: true);

                eventAggregator.GetEvent<SignalFadedEvent>().Subscribe(
                    action: _ => OnSignalFaded(),
                    keepSubscriberReferenceAlive: true);
            }
        }

        public void Pause()
        {
            if (playingProvider != default)
            {
                fadingProvider.Fade(
                    volume: FadingVolunmeDown,
                    duration: FadeDuration);

                IsPlaying = false;
            }
        }

        public void Start()
        {
            if (playingProvider != default)
            {
                playingProvider.Start();

                var fadingDuration = IsStarted
                    ? FadeDuration
                    : TimeSpan.Zero;

                fadingProvider.Fade(
                    volume: FadingVolunmeUp,
                    duration: fadingDuration);

                IsStarted = true;
                IsPlaying = true;

                eventAggregator.GetEvent<SignalStartedEvent>().Publish(track);
            }
        }

        public void Stop()
        {
            if (playingProvider != default)
            {
                playingProvider.Pause();

                IsPlaying = false;

                eventAggregator.GetEvent<SignalStoppedEvent>().Publish(track);
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
                    End();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnSignalFaded()
        {
            if (fadingProvider?.Volume == FadingVolunmeDown)
            {
                Stop();
            }
        }

        private void OnSignalUpdated()
        {
            if (!selectNextThresholdExceeded
                && Position.Ticks > selectNextThreshold.Ticks)
            {
                selectNextThresholdExceeded = true;

                trackService.Next();
            }
        }

        #endregion Private Methods
    }
}