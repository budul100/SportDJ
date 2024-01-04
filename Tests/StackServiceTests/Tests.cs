using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System;
using System.Linq;
using Xunit;

namespace SportDJ.Tests.StackServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly Mock<IDispatcherService> mockDispatcherService;
        private readonly Mock<IEventAggregator> mockEventAggregator;
        private readonly Mock<IStorageService<Set>> mockSettingsService;
        private readonly Services.TrackService.Service trackService;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            mockEventAggregator = new Mock<IEventAggregator>();

            var mockTrackSelectedEvent = new Mock<SignalUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<SignalUpdatedEvent>()).
                Returns(mockTrackSelectedEvent.Object);

            var mockTrackUpdatedEvent = new Mock<TrackUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<TrackUpdatedEvent>()).
                Returns(mockTrackUpdatedEvent.Object);

            mockDispatcherService = new Mock<IDispatcherService>();
            mockDispatcherService
                .Setup(m => m.InvokeAsync(It.IsAny<Action>()))
                .Callback<Action>(a => a.Invoke());

            mockSettingsService = new Mock<IStorageService<Set>>();
            mockSettingsService
                .Setup(m => m.Contents)
                .Returns(new Set());

            trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                dispatcherService: mockDispatcherService.Object,
                settingsService: mockSettingsService.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void DecreasePlayeds()
        {
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                trackService: trackService);

            stackService.Initialize(@"..\..\..\Samples");

            var track = stackService.Track;

            stackService.Play();
            Assert.True(stackService.HasStack);
            Assert.True(stackService.Track == track);

            stackService.Next();
            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();
            Assert.False(stackService.HasStack);
            Assert.True(stackService.Track == track);

            stackService.Next();
            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();
            Assert.True(stackService.HasStack);
            Assert.True(stackService.Track == track);
        }

        [Fact]
        public void LoadFiles()
        {
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                trackService: trackService);

            stackService.Initialize(@"..\..\..\Samples");

            Assert.Equal(
                expected: 3,
                actual: stackService.Tracks.Count(t => t.Trimmed));

            Assert.True(stackService.Tracks.All(t => t.Start > TimeSpan.Zero));
            Assert.True(stackService.Tracks.All(t => t.Length > TimeSpan.Zero));

            Assert.True(stackService.Track != default);
        }

        [Fact]
        public void MaintainOnReset()
        {
            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                trackService: trackService);

            stackService.Initialize(@"..\..\..\Samples");

            var track = stackService.Track;

            stackService.Play();
            Assert.True(stackService.HasStack);
            Assert.True(stackService.Track == track);

            stackService.Next();
            Assert.False(stackService.Track == track);

            track = stackService.Track;

            stackService.Play();
            Assert.False(stackService.HasStack);
            Assert.True(stackService.Track == track);

            stackService.Reset();
            Assert.True(stackService.HasStack);
            Assert.True(stackService.Track == track);
        }

        #endregion Public Methods
    }
}