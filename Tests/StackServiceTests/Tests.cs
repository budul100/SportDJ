using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using Xunit;

namespace SportDJ.Tests.StackServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly Mock<IDispatcherService> mockDispatcherService;
        private readonly Mock<IEventAggregator> mockEventAggregator;
        private readonly Mock<IStorageService<Session>> mockSessionService;
        private readonly Mock<IStorageService<Set>> mockSettingsService;
        private readonly Mock<TrackUpdatedEvent> trackUpdatedEvent;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            var options = new Options
            {
                TrimRearPercentage = 50,
                TrimVolumePercentage = 30,
            };

            var set = new Set
            {
                Options = options,
            };

            mockSettingsService = new Mock<IStorageService<Set>>();
            mockSettingsService
                .Setup(m => m.Contents)
                .Returns(set);

            mockEventAggregator = new Mock<IEventAggregator>();

            var signalUpdatedEvent = new Mock<SignalUpdatedEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<SignalUpdatedEvent>())
                .Returns(signalUpdatedEvent.Object);

            var entryUpdatedEvent = new Mock<EntryUpdatedEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<EntryUpdatedEvent>())
                .Returns(entryUpdatedEvent.Object);

            var stackResetEvent = new Mock<StackResetEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<StackResetEvent>())
                .Returns(stackResetEvent.Object);

            var stackUpdatedEvent = new Mock<StackUpdatedEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<StackUpdatedEvent>())
                .Returns(stackUpdatedEvent.Object);

            trackUpdatedEvent = new Mock<TrackUpdatedEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<TrackUpdatedEvent>())
                .Returns(trackUpdatedEvent.Object);

            mockDispatcherService = new Mock<IDispatcherService>();
            mockDispatcherService
                .Setup(m => m.InvokeAsync(It.IsAny<Action>()))
                .Callback<Action>(a => a.Invoke());

            mockSessionService = new Mock<IStorageService<Session>>();
            mockSessionService
                .Setup(m => m.Contents)
                .Returns(new Session());
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void DecreasePlayeds()
        {
            var trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                settingsService: mockSettingsService.Object);

            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: [@"..\..\..\Samples"],
                name: default,
                resetPosition: true,
                isLoop: false);

            var track = stackService.Track;

            Assert.Equal(
                expected: 3,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            stackService.Play();

            Assert.Equal(
                expected: 2,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);

            stackService.Next();

            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();

            Assert.Equal(
                expected: 1,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);

            stackService.Next();

            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();

            Assert.Equal(
                expected: 2,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);
        }

        [Fact]
        public void LoadFiles()
        {
            var tracksHandled = 0;

            trackUpdatedEvent
                .Setup(e => e.Publish(It.IsAny<Track>()))
                .Callback(() => tracksHandled++);

            var trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                settingsService: mockSettingsService.Object);

            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: [@"..\..\..\Samples"],
                name: default,
                resetPosition: true,
                isLoop: false);

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 5000)
            {
                if (tracksHandled >= 6) { break; }
                Thread.Sleep(200);
            }

            Assert.Equal(
                expected: 3,
                actual: stackService.Entries.Count);

            Assert.True(stackService.Entries.All(e => e.Track.Start > 0));
            Assert.True(stackService.Entries.All(e => e.Track.End > 0));
            Assert.True(stackService.Entries.All(e => e.Track.Cue < e.Track.End));

            Assert.True(stackService.Track != default);
        }

        [Fact]
        public void MaintainOnReset()
        {
            var trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                settingsService: mockSettingsService.Object);

            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: [@"..\..\..\Samples"],
                name: default,
                resetPosition: true,
                isLoop: false);

            var track = stackService.Track;

            Assert.Equal(
                expected: 3,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            stackService.Play();

            Assert.Equal(
                expected: 2,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);

            stackService.Next();

            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();

            Assert.Equal(
                expected: 1,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);

            stackService.Reset();

            Assert.Equal(
                expected: 3,
                actual: stackService.Entries.Count(e => !e.IsPlayed));

            Assert.True(stackService.Track == track);
        }

        #endregion Public Methods
    }
}