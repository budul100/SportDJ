using Moq;
using Prism.Events;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Signal;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System;
using System.Linq;
using Xunit;

namespace SportDJ.Tests.TrackServiceTests;

public class Tests
{
    #region Private Fields

    private readonly Mock<IDispatcherService> mockDispatcherService;
    private readonly Mock<IEventAggregator> mockEventAggregator;
    private readonly Mock<IStorageService<Session>> mockSessionService;
    private readonly Mock<IStorageService<Set>> mockSettingsService;

    #endregion Private Fields

    #region Public Constructors

    public Tests()
    {
        var options = new Options
        {
            TrimRearPercentage = 50,
            TrimVolumePercentage = 30,
        };

        var set = new Set
        {
            Options = options,
        };

        mockSettingsService = new Mock<IStorageService<Set>>();
        mockSettingsService
            .Setup(m => m.Contents)
            .Returns(set);

        mockEventAggregator = new Mock<IEventAggregator>();

        var mockTrackSelectedEvent = new Mock<SignalUpdatedEvent>();
        mockEventAggregator.
            Setup(m => m.GetEvent<SignalUpdatedEvent>()).
            Returns(mockTrackSelectedEvent.Object);

        var mockTrackUpdatedEvent = new Mock<EntryUpdatedEvent>();
        mockEventAggregator.
            Setup(m => m.GetEvent<EntryUpdatedEvent>()).
            Returns(mockTrackUpdatedEvent.Object);

        var trackUpdatedEvent = new Mock<TrackUpdatedEvent>();
        mockEventAggregator.
            Setup(m => m.GetEvent<TrackUpdatedEvent>()).
            Returns(trackUpdatedEvent.Object);

        mockDispatcherService = new Mock<IDispatcherService>();
        mockDispatcherService
            .Setup(m => m.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a.Invoke());

        mockSessionService = new Mock<IStorageService<Session>>();
        mockSessionService
            .Setup(m => m.Contents)
            .Returns(new Session());
    }

    #endregion Public Constructors

    #region Public Methods

    [Fact]
    public void SearchTracks()
    {
        var trackService = new Services.TrackService.Service(
            eventAggregator: mockEventAggregator.Object,
            settingsService: mockSettingsService.Object);

        var tracks = trackService.Get(
            paths: [@"..\..\..\Samples"]).ToArray();

        Assert.Equal(3, tracks.Length);

        var result1 = trackService.Search(" ");

        Assert.False(result1?.Count() > 0);

        var result2 = trackService.Search("3");

        Assert.Single(result2);

        var result3 = trackService.Search("sample");

        Assert.Equal(3, result3.Count());
    }

    #endregion Public Methods
}