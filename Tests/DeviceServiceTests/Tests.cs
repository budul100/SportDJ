using System.Linq;
using Moq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Mixer;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using Xunit;

namespace SportDJ.Tests.DeviceServiceTests;

public class Tests
{
    #region Private Fields

    private readonly Mock<IEventAggregator> mockEventAggregator;
    private readonly Mock<IStorageService<Session>> mockSessionService;
    private readonly Mock<IStorageService<Set>> mockSettingsService;

    #endregion Private Fields

    #region Public Constructors

    public Tests()
    {
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

        var mockUpdatedEvent = new Mock<DevicesUpdatedEvent>();
        mockEventAggregator
            .Setup(m => m.GetEvent<DevicesUpdatedEvent>())
            .Returns(mockUpdatedEvent.Object);

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
        var service = new Services.DeviceService.Windows.Service(
            eventAggregator: mockEventAggregator.Object);

        service.Update();

        Assert.True(service.Devices.Any());
    }

    #endregion Public Methods
}
