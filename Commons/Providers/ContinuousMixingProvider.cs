using System;
using System.Collections.Generic;
using NAudio.Utils;
using NAudio.Wave;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Providers
{
    public class ContinuousMixingProvider
        : BaseSampleProvider
    {
        #region Private Fields

        private readonly object lockProviders = new object();
        private readonly object lockReader = new object();
        private readonly List<ISampleProvider> sourceProviders = new List<ISampleProvider>();

        private float[] sourceBuffer;

        #endregion Private Fields

        #region Public Constructors

        public ContinuousMixingProvider(WaveFormat waveFormat)
            : base(waveFormat: waveFormat)
        { }

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

            lock (lockProviders)
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

            lock (lockReader)
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

                    outputSamples = Math.Max(
                        val1: samplesRead,
                        val2: outputSamples);

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
                lock (lockProviders)
                {
                    sourceProviders.Remove(source);
                }
            }
        }

        public void RemoveAll()
        {
            lock (lockProviders)
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