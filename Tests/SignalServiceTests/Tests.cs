using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Moq;
using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.SignalService.Windows;
using SportDJ.Shareds.SampleProviders.Windows;
using SportDJ.Tests.SignalServiceTests.Helpers;
using Xunit;

namespace SportDJ.Tests.SignalServiceTests;

public class Tests
{
    #region Private Fields

    private const int BufferSamples = 4410 * 2;

    // A read must stay far below the blocking time of the subscriber
    private const int MaxReadMilliseconds = 100;
    private const int ReadCount = 20;
    // Simulated duration of a slow track selection (loading and decoding the next file)
    private const int SelectNextBlockMilliseconds = 300;

    #endregion Private Fields

    #region Public Methods

    [Fact]
    public void Read_SlowSelectNextSubscriber_DoesNotBlockAudioThread()
    {
        var path = WavFileFactory.CreateSilent(TimeSpan.FromSeconds(5));

        try
        {
            var eventAggregator = new EventAggregator();

            eventAggregator.GetEvent<SignalNextEvent>().Subscribe(
                action: _ => Thread.Sleep(SelectNextBlockMilliseconds),
                threadOption: ThreadOption.PublisherThread);

            using var service = CreateService(eventAggregator, path);

            var maxRead = ReadOnAudioThread(service, out _);

            Assert.True(maxRead < TimeSpan.FromMilliseconds(MaxReadMilliseconds),
                $"A read took {maxRead.TotalMilliseconds:F0} ms, the audio thread was blocked.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_TrackBelowThreshold_PublishesSelectNextOutsideAudioThread()
    {
        var path = WavFileFactory.CreateSilent(TimeSpan.FromSeconds(5));

        try
        {
            var eventAggregator = new EventAggregator();
            using var published = new ManualResetEventSlim();
            var publisherThreadId = -1;

            eventAggregator.GetEvent<SignalNextEvent>().Subscribe(
                action: _ =>
                {
                    publisherThreadId = Environment.CurrentManagedThreadId;
                    published.Set();
                },
                threadOption: ThreadOption.PublisherThread);

            using var service = CreateService(eventAggregator, path);

            ReadOnAudioThread(service, out var audioThreadId);

            Assert.True(published.Wait(TimeSpan.FromSeconds(5)), "SignalNextEvent was never published.");
            Assert.NotEqual(audioThreadId, publisherThreadId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion Public Methods

    #region Private Methods

    private static Service CreateService(IEventAggregator eventAggregator, string path)
    {
        var settingsService = new Mock<IStorageService<Set>>();

        settingsService.SetupGet(s => s.Contents).Returns(new Set());

        var service = new Service(
            eventAggregator: eventAggregator,
            settingsService: settingsService.Object,
            trackService: new Mock<ITrackService>().Object,
            sourceProvider: new SourceProvider());

        service.Initialize(
            track: new Track(path, Path.GetFileName(path)),
            waveFormat: WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        service.Start();

        return service;
    }

    /// <summary>
    /// Reads the output like the audio device does, on a dedicated thread. Returns the longest single read.
    /// </summary>
    private static TimeSpan ReadOnAudioThread(ISignalService service, out int audioThreadId)
    {
        var maxRead = TimeSpan.Zero;
        var threadId = -1;

        var thread = new Thread(() =>
        {
            threadId = Environment.CurrentManagedThreadId;

            var buffer = new float[BufferSamples];

            for (var i = 0; i < ReadCount; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                service.Output.Read(buffer);

                stopwatch.Stop();

                if (stopwatch.Elapsed > maxRead)
                {
                    maxRead = stopwatch.Elapsed;
                }
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        thread.Join();

        audioThreadId = threadId;

        return maxRead;
    }

    #endregion Private Methods
}
