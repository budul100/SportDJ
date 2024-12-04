using System;
using NAudio.Wave;

namespace SportDJ.Commons.Providers
{
    public class EchoEffectProvider(ISampleProvider sourceProvider)
        : BaseSampleProvider(waveFormat: sourceProvider?.WaveFormat)
    {
        #region Private Fields

        private readonly object lockProvider = new();

        private TimeSpan delay;
        private float[] delayBuffer;
        private int delayBufferLength;
        private int delayBufferOffset;
        private float feedback;
        private float wetDryMix;

        #endregion Private Fields

        #region Public Methods

        public void Activate(float wetDryMix, float feedback, int delayMilliSeconds)
        {
            if (wetDryMix <= 0 || wetDryMix > 1)
            {
                throw new ArgumentException("The value must be greater than 0 and less or equal to 1.", nameof(wetDryMix));
            }

            if (feedback <= 0 || feedback > 1)
            {
                throw new ArgumentException("The value must be greater than 0 and and less or equal to 1.", nameof(feedback));
            }

            if (delayMilliSeconds <= 0)
            {
                throw new ArgumentException("The value must be greater than 0.", nameof(delayMilliSeconds));
            }

            lock (lockProvider)
            {
                this.wetDryMix = wetDryMix;
                this.feedback = feedback;

                delay = TimeSpan.FromMilliseconds(delayMilliSeconds);

                delayBufferLength = (int)(WaveFormat.SampleRate * WaveFormat.Channels * delay.TotalSeconds);
                delayBuffer = new float[delayBufferLength];
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = sourceProvider.Read(
                buffer: buffer,
                offset: offset,
                count: count);

            if (wetDryMix > 0)
            {
                lock (lockProvider)
                {
                    SetEcho(
                        buffer: buffer,
                        offset: offset,
                        count: samplesRead);
                }
            }

            return samplesRead;
        }

        public void Reset()
        {
            wetDryMix = 0;
        }

        #endregion Public Methods

        #region Private Methods

        private void SetEcho(float[] buffer, int offset, int count)
        {
            for (var sample = 0; sample < count; sample++)
            {
                var drySample = buffer[offset + sample];
                var wetSample = delayBuffer[delayBufferOffset];

                buffer[offset + sample] = (drySample * (1 - wetDryMix)) + (wetSample * wetDryMix);

                delayBuffer[delayBufferOffset] = drySample + (wetSample * feedback);

                delayBufferOffset++;

                if (delayBufferOffset >= delayBufferLength)
                {
                    delayBufferOffset = 0;
                }
            }
        }

        #endregion Private Methods
    }
}