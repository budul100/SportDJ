using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SportDJ.Commons.Models.Contents;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core.Extensions;

namespace SportDJ.Shareds.SampleProviders.Windows
{
    public class LoopingProvider
        : IPlayingProvider
    {
        #region Private Fields

        private readonly int averageBytesPerSecond;
        private readonly object lockAnalysis = new();
        private readonly object lockLoop = new();
        private readonly ISourceProvider sourceProvider;
        private readonly Track track;

        private float[] allSamples;
        private long endBytes;
        private bool isDisposed;
        private long startBytes;

        #endregion Private Fields

        #region Public Constructors

        public LoopingProvider(ISourceProvider sourceProvider, Track track)
        {
            this.sourceProvider = sourceProvider;
            this.track = track;

            averageBytesPerSecond = WaveFormat.AverageBytesPerSecond;

            Task.Run(InitializeAnalysis);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler LoopActivateEvent;

        public event EventHandler LoopDeactivateEvent;

        public event EventHandler LoopReadyEvent;

        #endregion Public Events

        #region Public Properties

        public TimeSpan? End => IsLoop
            ? endBytes.ToDuration(averageBytesPerSecond)
            : default;

        public bool IsLoop { get; private set; }

        public bool IsReady { get; private set; }

        public long Length => sourceProvider.Length;

        public long Position
        {
            get => sourceProvider.Position;
            set => sourceProvider.Position = value;
        }

        public TimeSpan? Start => IsLoop
            ? startBytes.ToDuration(averageBytesPerSecond)
            : default;

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        #endregion Public Properties

        #region Public Methods

        public void Activate()
        {
            lock (lockAnalysis)
            {
                if (IsReady && allSamples?.Length > 0)
                {
                    var bytesPerSample = WaveFormat.BlockAlign;
                    var currentSample = Position / bytesPerSample;

                    var (Start, End) = allSamples.DetectLoop(
                        position: currentSample,
                        sampleRate: WaveFormat.SampleRate);

                    if (Start.HasValue && End.HasValue && End.Value > Start.Value)
                    {
                        lock (lockLoop)
                        {
                            startBytes = Start.Value * bytesPerSample;
                            endBytes = End.Value * bytesPerSample;

                            IsLoop = true;
                        }

                        LoopActivateEvent?.Invoke(
                            sender: this,
                            e: default);
                    }
                }
            }
        }

        public void Deactivate()
        {
            if (IsLoop)
            {
                lock (lockLoop)
                {
                    IsLoop = false;
                }

                LoopDeactivateEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = 0;

            lock (lockLoop)
            {
                if (IsLoop)
                {
                    var bytesPerSample = WaveFormat.BlockAlign;

                    while (samplesRead < count)
                    {
                        var samplesNeeded = count - samplesRead;

                        var bytesUntilEnd = endBytes - Position;
                        var samplesUntilEnd = (int)(bytesUntilEnd / bytesPerSample);

                        if (samplesUntilEnd <= 0)
                        {
                            Position = startBytes;
                            bytesUntilEnd = endBytes - startBytes;
                            samplesUntilEnd = (int)(bytesUntilEnd / bytesPerSample);
                        }

                        var toRead = Math.Min(samplesNeeded, samplesUntilEnd);

                        if (toRead <= 0)
                        {
                            break;
                        }

                        var samplesLoop = sourceProvider.Read(
                            buffer: buffer,
                            offset: offset + samplesRead,
                            count: toRead);

                        if (samplesLoop == 0)
                        {
                            break;
                        }

                        samplesRead += samplesLoop;
                    }
                }
                else
                {
                    samplesRead = sourceProvider.Read(
                        buffer: buffer,
                        offset: offset,
                        count: count);
                }
            }

            return samplesRead;
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    sourceProvider?.Dispose();

                    lock (lockAnalysis)
                    {
                        allSamples = null;
                    }
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private float[] GetSamples()
        {
            var result = default(float[]);

            try
            {
                using var stream = new FileStream(
                    path: track.Path,
                    mode: FileMode.Open,
                    access: FileAccess.Read,
                    share: FileShare.Read);

                using var reader = new StreamMediaFoundationReader(stream);

                var sampleProvider = reader.ToSampleProvider();

                var monoProvider = reader.WaveFormat.Channels == 1
                    ? sampleProvider
                    : new StereoToMonoSampleProvider(sampleProvider);

                var buffer = new List<float>();
                var readBuffer = new float[reader.WaveFormat.SampleRate];

                int read;

                while (!isDisposed
                    && (read = monoProvider.Read(
                        buffer: readBuffer,
                        offset: 0,
                        count: readBuffer.Length)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        buffer.Add(readBuffer[i]);
                    }
                }

                result = buffer.ToArray();
            }
            catch
            { }

            return result;
        }

        private async void InitializeAnalysis()
        {
            var samples = await Task.Run(GetSamples);

            lock (lockAnalysis)
            {
                allSamples = samples;
                IsReady = allSamples?.Length > 0;

                if (IsReady)
                {
                    LoopReadyEvent?.Invoke(
                        sender: this,
                        e: default);
                }
            }
        }

        #endregion Private Methods
    }
}