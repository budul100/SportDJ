using Moq;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System.Linq;
using Xunit;

namespace SportDJ.Tests.OutputServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly Mock<IContainerProvider> mockContainerProvider;

        private readonly Mock<IEventAggregator> mockEventAggregator;
        private readonly Mock<IStorageService<Set>> mockSettingsService;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            mockContainerProvider = new Mock<IContainerProvider>();

            mockEventAggregator = new Mock<IEventAggregator>();

            var mockPlayerStartedEvent = new Mock<PlayerStartedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<PlayerStartedEvent>()).
                Returns(mockPlayerStartedEvent.Object);

            var mockPlayerStoppedEvent = new Mock<PlayerStoppedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<PlayerStoppedEvent>()).
                Returns(mockPlayerStoppedEvent.Object);

            mockSettingsService = new Mock<IStorageService<Set>>();
            mockSettingsService
                .Setup(m => m.Contents)
                .Returns(new Set());
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void GetDevices()
        {
            var service = new Services.OutputService.Service(
                mockContainerProvider.Object,
                mockEventAggregator.Object,
                mockSettingsService.Object);

            var devices = service.GetDevices();

            Assert.True(devices.Any());
        }

        #endregion Public Methods
    }
}