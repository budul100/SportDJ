using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SportDJ.Commons.Models.Contents;
using SportDJ.Shareds.SampleProviders.Abstractions;
using SportDJ.Shareds.SampleProviders.Core.Extensions;

namespace SportDJ.Shareds.SampleProviders.Windows;

public class LoopingProvider
    : IPlayingProvider
{
    #region Private Fields

    private readonly Lock lockAnalysis = new();
    private readonly Lock lockLoop = new();
    private readonly ISourceProvider sourceProvider;
    private readonly Track track;

    private float[] allSamples;
    private long endSample;
    private bool isDisposed;
    private long startSample;

    #endregion Private Fields

    #region Public Constructors

    public LoopingProvider(ISourceProvider sourceProvider, Track track)
    {
        this.sourceProvider = sourceProvider;
        this.track = track;

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
        ? endSample.ToDuration(WaveFormat.SampleRate)
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
        ? startSample.ToDuration(WaveFormat.SampleRate)
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
                var currentSample = Position;

                var (Start, End) = allSamples.DetectLoop(
                    position: currentSample,
                    sampleRate: WaveFormat.SampleRate);

                if (Start.HasValue
                    && End.HasValue
                    && End.Value > Start.Value)
                {
                    lock (lockLoop)
                    {
                        startSample = Start.Value;
                        endSample = End.Value;

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

    public int Read(Span<float> buffer)
    {
        var count = buffer.Length;
        var samplesRead = 0;

        lock (lockLoop)
        {
            if (IsLoop)
            {
                var channels = WaveFormat.Channels;

                while (samplesRead < count)
                {
                    var samplesNeeded = count - samplesRead;

                    // Position and loop bounds are in frames, the buffer is in interleaved samples
                    var samplesUntilEnd = (endSample - Position) * channels;

                    if (samplesUntilEnd <= 0)
                    {
                        Position = startSample;
                        samplesUntilEnd = (endSample - startSample) * channels;
                    }

                    var toRead = (int)Math.Min(samplesNeeded, samplesUntilEnd);

                    if (toRead <= 0)
                    {
                        break;
                    }

                    var samplesLoop = sourceProvider.Read(buffer.Slice(samplesRead, toRead));

                    if (samplesLoop == 0)
                    {
                        break;
                    }

                    samplesRead += samplesLoop;
                }
            }
            else
            {
                samplesRead = sourceProvider.Read(buffer);
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
            // Decode with the same reader as the playback (SourceProvider), so the analysis
            // timeline matches the played samples exactly. Another decoder (e.g. Media Foundation)
            // can differ in encoder delay and would shift the loop points.
            using var reader = new AudioFileReader(track.Path);

            ISampleProvider sampleProvider = reader;

            var monoProvider = reader.WaveFormat.Channels == 1
                ? sampleProvider
                : new StereoToMonoSampleProvider(sampleProvider);

            var buffer = new List<float>();
            var readBuffer = new float[reader.WaveFormat.SampleRate];

            var samplesRead = 0;

            while (!isDisposed
                && (samplesRead = monoProvider.Read(readBuffer)) > 0)
            {
                buffer.AddRange(readBuffer.AsSpan(0, samplesRead));
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
