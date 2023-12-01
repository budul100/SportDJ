using Moq;
using Prism.Events;
using Prism.Ioc;
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
        private readonly Mock<ISettingsService<Session>> mockSettingsService;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            mockContainerProvider = new Mock<IContainerProvider>();

            mockEventAggregator = new Mock<IEventAggregator>();

            mockSettingsService = new Mock<ISettingsService<Session>>();
            mockSettingsService
                .Setup(m => m.Settings.Options)
                .Returns(new Options());
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