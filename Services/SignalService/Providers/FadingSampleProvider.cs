using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Models.Contents;
using SportDJ.Services.SignalService.Enums;
using SportDJ.Services.SignalService.Extensions;
using System;

namespace SportDJ.Services.SignalService.Providers
{
    internal class FadingSampleProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private long fadeSampleCount;
        private long fadeSamplePosition;
        private FadeType fadeType;
        private float volumeCurrent;
        private float volumeFadeDiff;
        private float volumeFadeStart;

        #endregion Private Fields

        #region Public Constructors

        public FadingSampleProvider(IEventAggregator eventAggregator, ISampleProvider source, Track track,
            float volume = 0)
            : base(eventAggregator, source, track)
        {
            Volume = volume;
            volumeCurrent = Volume;
        }

        #endregion Public Constructors

        #region Public Properties

        public float Volume { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Fade(float volume, TimeSpan? duration = default)
        {
            fadeSampleCount = duration.ToLength(averageBytesPerSecond);
            Volume = volume;

            StartFade(Volume);
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(
                buffer: buffer,
                offset: offset,
                count: count);

            lock (source)
            {
                AdjustBuffer(
                    buffer: buffer,
                    offset: offset,
                    count: read);
            }

            return read;
        }

        #endregion Public Methods

        #region Private Methods

        private void AdjustBuffer(float[] buffer, int offset, int count)
        {
            var sample = 0;

            while (sample < count)
            {
                if (fadeType == FadeType.FadingIn)
                {
                    volumeCurrent = volumeFadeStart + (fadeSamplePosition * volumeFadeDiff / fadeSampleCount);
                }
                else if (fadeType == FadeType.FadingOut)
                {
                    volumeCurrent = volumeFadeStart - (fadeSamplePosition * volumeFadeDiff / fadeSampleCount);
                }

                for (var ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= volumeCurrent;
                }

                fadeSamplePosition++;

                if (fadeType != FadeType.Full
                    && fadeSamplePosition > fadeSampleCount)
                {
                    fadeType = FadeType.Full;
                    eventAggregator.GetEvent<SignalFadedEvent>().Publish(track);
                }
            }
        }

        private void StartFade(float volume)
        {
            if (fadeSampleCount == 0)
            {
                volumeCurrent = volume;
                eventAggregator.GetEvent<SignalFadedEvent>().Publish(track);
            }
            else
            {
                if (volume > volumeCurrent)
                {
                    fadeType = FadeType.FadingIn;
                    volumeFadeStart = volumeCurrent;
                    volumeFadeDiff = volume - volumeCurrent;
                }
                else
                {
                    fadeType = FadeType.FadingOut;
                    volumeFadeStart = volumeCurrent;
                    volumeFadeDiff = volumeCurrent - volume;
                }

                lock (source)
                {
                    fadeSamplePosition = 0;
                }
            }
        }

        #endregion Private Methods
    }
}