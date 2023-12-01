using NAudio.Utils;
using NAudio.Wave;
using Score2Stream.Commons;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Providers
{
    public class MixingSampleProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly List<ISampleProvider> sourceProviders = new();

        private float[] sourceBuffer;

        #endregion Private Fields

        #region Public Constructors

        public MixingSampleProvider(WaveFormat waveFormat)
            : base(waveFormat: waveFormat)
        {
            if (WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException(
                    message: "Mixing sample provider format must be a floating point format.",
                    paramName: nameof(waveFormat));
            }
        }

        #endregion Public Constructors

        #region Public Methods

        public void Add(ISampleProvider sourceProvider)
        {
            if (WaveFormat.SampleRate != sourceProvider.WaveFormat.SampleRate
                || WaveFormat.Channels != sourceProvider.WaveFormat.Channels)
            {
                throw new ArgumentException(
                    message: "All mixer inputs must have the same wave format.",
                    paramName: nameof(sourceProvider));
            }

            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sourceProviders)
            {
                if (sourceProviders.Count >= Constants.MixerSourcesMax)
                {
                    throw new InvalidOperationException(
                        $"There cannot be more than {Constants.MixerSourcesMax} mixer inputs.");
                }

                sourceProviders.Add(sourceProvider);
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var outputSamples = 0;

            sourceBuffer = BufferHelpers.Ensure(
                buffer: sourceBuffer,
                samplesRequired: count);

            lock (sourceProviders)
            {
                var index = sourceProviders.Count - 1;

                while (index >= 0)
                {
                    var source = sourceProviders[index];
                    var samplesRead = source.Read(
                        buffer: sourceBuffer,
                        offset: 0,
                        count: count);

                    var outIndex = offset;

                    for (var n = 0; n < samplesRead; n++)
                    {
                        if (n >= outputSamples)
                        {
                            buffer[outIndex++] = sourceBuffer[n];
                        }
                        else
                        {
                            buffer[outIndex++] += sourceBuffer[n];
                        }
                    }

                    outputSamples = Math.Max(samplesRead, outputSamples);

                    if (samplesRead == 0
                        && (sourceProviders?.Count ?? 0) > index + 1)
                    {
                        sourceProviders.RemoveAt(index);
                    }

                    index--;
                }
            }

            if (outputSamples < count)
            {
                var outputIndex = offset + outputSamples;

                while (outputIndex < offset + count)
                {
                    buffer[outputIndex++] = 0;
                }

                outputSamples = count;
            }

            return outputSamples;
        }

        public void Remove(ISampleProvider source)
        {
            if (sourceProviders.Contains(source))
            {
                lock (this.sourceProviders)
                {
                    this.sourceProviders.Remove(source);
                }
            }
        }

        public void RemoveAll()
        {
            lock (sourceProviders)
            {
                sourceProviders.Clear();
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            RemoveAll();

            base.Dispose(isDisposing);
        }

        #endregion Protected Methods
    }
}