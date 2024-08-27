using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
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
        private readonly Services.TrackService.Service trackService;

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

            var mockTrackSelectedEvent = new Mock<SignalUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<SignalUpdatedEvent>()).
                Returns(mockTrackSelectedEvent.Object);

            var mockTrackUpdatedEvent = new Mock<EntryUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<EntryUpdatedEvent>()).
                Returns(mockTrackUpdatedEvent.Object);

            var trackUpdatedEvent = new Mock<TrackUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<TrackUpdatedEvent>()).
                Returns(trackUpdatedEvent.Object);

            mockDispatcherService = new Mock<IDispatcherService>();
            mockDispatcherService
                .Setup(m => m.InvokeAsync(It.IsAny<Action>()))
                .Callback<Action>(a => a.Invoke());

            mockSessionService = new Mock<IStorageService<Session>>();
            mockSessionService
                .Setup(m => m.Contents)
                .Returns(new Session());

            trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                settingsService: mockSettingsService.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void DecreasePlayeds()
        {
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: new string[] { @"..\..\..\Samples" },
                name: default,
                savePosition: false,
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
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: new string[] { @"..\..\..\Samples" },
                name: default,
                savePosition: false,
                isLoop: false);

            Task.Delay(3000).Wait();

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
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                sessionService: mockSessionService.Object,
                trackService: trackService);

            stackService.Initialize(
                paths: new string[] { @"..\..\..\Samples" },
                name: default,
                savePosition: false,
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