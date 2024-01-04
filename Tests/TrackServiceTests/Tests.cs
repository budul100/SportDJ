using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Signals;
using SportDJ.Commons.Events.Tracks;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.TrackService;
using System;
using System.Linq;
using Xunit;

namespace SportDJ.Tests.TrackServiceTests
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

            var mockTrackUpdatedEvent = new Mock<TracksUpdatedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<TracksUpdatedEvent>()).
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
            var trackService = new Service(mockEventAggregator.Object, mockDispatcherService.Object, mockSettingsService.Object);

            trackService.Initialize(@"..\..\..\Samples");

            Assert.True(trackService.Tracks.Count(t => t.Trimmed) == 3);

            Assert.True(trackService.Tracks.All(t => t.Start > TimeSpan.Zero));
            Assert.True(trackService.Tracks.All(t => t.Length > TimeSpan.Zero));

            Assert.True(trackService.Track != default);
        }

        #endregion Public Methods
    }
}