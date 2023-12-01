using Moq;
using Prism.Events;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System.Linq;
using Xunit;

namespace SportDJ.Tests.OutputServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly Mock<IEventAggregator> mockEventAggregator;
        private readonly Mock<IStorageService<Set>> mockSettingsService;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            mockEventAggregator = new Mock<IEventAggregator>();

            mockSettingsService = new Mock<IStorageService<Set>>();
            mockSettingsService
                .Setup(m => m.Contents.Options)
                .Returns(new Options());
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void GetDevices()
        {
            var service = new Services.OutputService.Service(
                mockEventAggregator.Object,
                mockSettingsService.Object);

            var devices = service.GetDevices();

            Assert.True(devices.Any());
        }

        #endregion Public Methods
    }
}