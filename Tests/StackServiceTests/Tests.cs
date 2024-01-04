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
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void LoadFiles()
        {
            var trackService = new Services.TrackService.Service(
                eventAggregator: mockEventAggregator.Object,
                dispatcherService: mockDispatcherService.Object,
                settingsService: mockSettingsService.Object);

            var stackService = new Services.StackService.Service(
                eventAggregator: mockEventAggregator.Object,
                trackService: trackService);

            stackService.Initialize(@"..\..\..\Samples");

            Assert.True(stackService.Tracks.Count(t => t.Trimmed) == 3);

            Assert.True(stackService.Tracks.All(t => t.Start > TimeSpan.Zero));
            Assert.True(stackService.Tracks.All(t => t.Length > TimeSpan.Zero));

            Assert.True(stackService.Track != default);
        }

        #endregion Public Methods
    }
}