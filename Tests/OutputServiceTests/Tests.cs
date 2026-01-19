using System.Linq;
using Moq;
using Prism.Events;
using Prism.Ioc;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using Xunit;

namespace SportDJ.Tests.OutputServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly Mock<IContainerProvider> mockContainerProvider;
        private readonly Mock<IEventAggregator> mockEventAggregator;
        private readonly Mock<IStorageService<Session>> mockSessionService;
        private readonly Mock<IStorageService<Set>> mockSettingsService;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            mockContainerProvider = new Mock<IContainerProvider>();
            mockEventAggregator = new Mock<IEventAggregator>();

            var settingsLoadedEvent = new Mock<SettingsLoadedEvent<Set>>();
            mockEventAggregator.
                Setup(m => m.GetEvent<SettingsLoadedEvent<Set>>()).
                Returns(settingsLoadedEvent.Object);

            var mockMixerStartedEvent = new Mock<MixerStartedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<MixerStartedEvent>()).
                Returns(mockMixerStartedEvent.Object);

            var mockMixerStoppedEvent = new Mock<MixerStoppedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<MixerStoppedEvent>()).
                Returns(mockMixerStoppedEvent.Object);

            var mockMixerEndedEvent = new Mock<MixerEndedEvent>();
            mockEventAggregator.
                Setup(m => m.GetEvent<MixerEndedEvent>()).
                Returns(mockMixerEndedEvent.Object);

            var mockDevicesUpdatedEvent = new Mock<DevicesUpdatedEvent>();
            mockEventAggregator
                .Setup(m => m.GetEvent<DevicesUpdatedEvent>())
                .Returns(mockDevicesUpdatedEvent.Object);

            mockSettingsService = new Mock<IStorageService<Set>>();
            mockSettingsService
                .Setup(m => m.Contents)
                .Returns(new Set());

            mockSessionService = new Mock<IStorageService<Session>>();
            mockSessionService
                .Setup(m => m.Contents)
                .Returns(new Session());
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void GetDevices()
        {
            var provider = new Services.OutputService.Windows.Service(
                containerProvider: mockContainerProvider.Object,
                eventAggregator: mockEventAggregator.Object,
                settingsService: mockSettingsService.Object,
                sessionService: mockSessionService.Object);

            provider.Update();

            Assert.True(provider.Devices.Any());
        }

        #endregion Public Methods
    }
}