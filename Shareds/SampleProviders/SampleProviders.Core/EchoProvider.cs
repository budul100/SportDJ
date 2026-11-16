using System;
using System.Threading;
using NAudio.Wave;

namespace SportDJ.Shareds.SampleProviders.Core;

public class EchoProvider(ISampleProvider sourceProvider)
    : BaseProvider(waveFormat: sourceProvider?.WaveFormat)
{
    #region Private Fields

    private TimeSpan delay;
    private float[] delayBuffer;
    private int delayBufferLength;
    private int delayBufferOffset;
    private float feedback;
    private readonly Lock lockProvider = new();
    private float wetDryMix;

    #endregion Private Fields

    #region Public Methods

    public void Activate(float wetDryMix, float feedback, int delayMilliSeconds)
    {
        if (wetDryMix <= 0 || wetDryMix > 1)
        {
            throw new ArgumentException(
                "The value must be greater than 0 and less or equal to 1.",
                nameof(wetDryMix));
        }

        if (feedback <= 0 || feedback > 1)
        {
            throw new ArgumentException(
                "The value must be greater than 0 and and less or equal to 1.",
                nameof(feedback));
        }

        if (delayMilliSeconds <= 0)
        {
            throw new ArgumentException(
                "The value must be greater than 0.",
                nameof(delayMilliSeconds));
        }

        lock (lockProvider)
        {
            this.feedback = feedback;

            delay = TimeSpan.FromMilliseconds(delayMilliSeconds);

            // Whole frames only, so the channels stay aligned inside the ring buffer
            var frames = Math.Max(
                val1: 1,
                val2: (int)(WaveFormat.SampleRate * delay.TotalSeconds));

            delayBufferLength = frames * WaveFormat.Channels;
            delayBuffer = new float[delayBufferLength];

            // The old offset may exceed the length of the new buffer
            delayBufferOffset = 0;

            // Set last, so the audio thread never sees the mix without a matching buffer
            this.wetDryMix = wetDryMix;
        }
    }

    public override int Read(Span<float> buffer)
    {
        var samplesRead = sourceProvider.Read(buffer);

        lock (lockProvider)
        {
            if (wetDryMix > 0
                && delayBuffer != null)
            {
                SetEcho(
                    buffer: buffer[..samplesRead]);
            }
        }

        return samplesRead;
    }

    public void Reset()
    {
        lock (lockProvider)
        {
            wetDryMix = 0;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void SetEcho(Span<float> buffer)
    {
        for (var sample = 0; sample < buffer.Length; sample++)
        {
            var drySample = buffer[sample];
            var wetSample = delayBuffer[delayBufferOffset];

            buffer[sample] = drySample * (1 - wetDryMix) + wetSample * wetDryMix;

            delayBuffer[delayBufferOffset] = drySample + wetSample * feedback;

            delayBufferOffset++;

            if (delayBufferOffset >= delayBufferLength)
            {
                delayBufferOffset = 0;
            }
        }
    }

    #endregion Private Methods
}