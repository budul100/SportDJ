using System;
using System.Collections.Generic;
using NAudio.Utils;
using NAudio.Wave;
using SportDJ.Commons.Assets;

namespace SportDJ.Shareds.SampleProviders.Core
{
    public class MixingProvider(WaveFormat waveFormat)
        : BaseProvider(waveFormat: waveFormat)
    {
        #region Private Fields

        private readonly object lockProvider = new();
        private readonly List<ISampleProvider> sourceProviders = [];

        private float[] sourceBuffer;

        #endregion Private Fields

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

            lock (lockProvider)
            {
                if (sourceProviders.Count >= Constants.MixerSourcesMax)
                {
                    throw new InvalidOperationException(
                        $"There cannot be more than {Constants.MixerSourcesMax} mixer inputs.");
                }

                if (!sourceProviders.Contains(sourceProvider))
                {
                    sourceProviders.Add(sourceProvider);
                }
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            var samplesMerged = 0;

            sourceBuffer = BufferHelpers.Ensure(
                buffer: sourceBuffer,
                samplesRequired: count);

            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] = 0;
            }

            lock (lockProvider)
            {
                var index = sourceProviders.Count - 1;

                while (index >= 0)
                {
                    var source = sourceProviders[index];

                    var samplesRead = source.Read(
                        buffer: sourceBuffer,
                        offset: 0,
                        count: count);

                    for (var n = 0; n < samplesRead; n++)
                    {
                        buffer[offset + n] += sourceBuffer[n];
                    }

                    samplesMerged = Math.Max(
                        val1: samplesRead,
                        val2: samplesMerged);

                    index--;
                }
            }

            if (samplesMerged < count)
            {
                samplesMerged = count;
            }

            return samplesMerged;
        }

        public void Remove(ISampleProvider sourceProvider)
        {
            lock (lockProvider)
            {
                sourceProviders.Remove(sourceProvider);
            }
        }

        public void RemoveAll()
        {
            lock (lockProvider)
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