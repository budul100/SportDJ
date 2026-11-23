using System.Linq;
using Moq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using Xunit;

namespace SportDJ.Tests.GridServiceTests;

public class LoadingTests
{
    #region Public Methods

    [Fact]
    public void LoadEmptyGroup()
    {
        var cell = new Group
        {
            Column = 0,
            Row = 0
        };

        var contents = new Set
        {
            Grid = new Grid { Cells = [cell] },
        };

        var mockSettingsService = new Mock<IStorageService<Set>>();
        mockSettingsService
            .Setup(m => m.Contents)
            .Returns(contents);

        var settingsLoadedEvent = new SettingsLoadedEvent<Set>();

        var mockEventAggregator = new Mock<IEventAggregator>();
        mockEventAggregator
            .Setup(x => x.GetEvent<SettingsLoadedEvent<Set>>())
            .Returns(settingsLoadedEvent);
        mockEventAggregator
            .Setup(x => x.GetEvent<GridUpdatedEvent>())
            .Returns(new GridUpdatedEvent());

        var mockKeyListenService = new Mock<IKeyboardService>();
        var mockDispatcherService = new Mock<IDispatcherService>();

        var service = new Services.GridService.Service(
            eventAggregator: mockEventAggregator.Object,
            settingsService: mockSettingsService.Object,
            dialogService: default,
            keyListenService: mockKeyListenService.Object,
            dispatcherService: mockDispatcherService.Object);

        settingsLoadedEvent.Publish();

        Assert.True((service.Grid.Cells.Single() as Group).Grids.Single().Cells.Single() is Player);
    }

    [Fact]
    public void LoadEmptySet()
    {
        var mockSettingsService = new Mock<IStorageService<Set>>();
        mockSettingsService
            .Setup(m => m.Contents)
            .Returns(new Set());

        var settingsLoadedEvent = new SettingsLoadedEvent<Set>();

        var mockEventAggregator = new Mock<IEventAggregator>();
        mockEventAggregator
            .Setup(x => x.GetEvent<SettingsLoadedEvent<Set>>())
            .Returns(settingsLoadedEvent);
        mockEventAggregator
            .Setup(x => x.GetEvent<GridUpdatedEvent>())
            .Returns(new GridUpdatedEvent());

        var mockKeyListenService = new Mock<IKeyboardService>();
        var mockDispatcherService = new Mock<IDispatcherService>();

        var service = new Services.GridService.Service(
            eventAggregator: mockEventAggregator.Object,
            settingsService: mockSettingsService.Object,
            dialogService: default,
            keyListenService: mockKeyListenService.Object,
            dispatcherService: mockDispatcherService.Object);

        settingsLoadedEvent.Publish();

        Assert.True(service.Grid.Cells.Single() is Player);
    }

    #endregion Public Methods
}
