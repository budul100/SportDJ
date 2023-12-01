using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Signal;
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
        private readonly Mock<ISettingsService<Session>> mockSettingsService;

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

            mockSettingsService = new Mock<ISettingsService<Session>>();
            mockSettingsService
                .Setup(m => m.Settings)
                .Returns(new Session());
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void LoadFiles()
        {
            var trackService = new Service(mockEventAggregator.Object, mockDispatcherService.Object, mockSettingsService.Object);

            trackService.Load(@"..\..\..\Samples");

            Assert.True(trackService.Tracks.Count(t => t.Trimmed) == 2);
            Assert.True(trackService.Track != default);
        }

        #endregion Public Methods
    }
}