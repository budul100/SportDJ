using System.Linq;
using System.Threading.Tasks;
using Moq;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Tests.GridServiceTests;

/// <summary>
/// Creates a grid service on top of mocked dependencies and loads the given grid like the settings service would do.
/// </summary>
internal sealed class ServiceContext
{
    #region Private Fields

    private readonly SettingsLoadedEvent<Set> loadedEvent = new();
    private readonly Mock<IStorageService<Set>> storage = new();

    private int saveCountAtLoad;
    private int updateCount;
    private int updateCountAtLoad;

    #endregion Private Fields

    #region Public Constructors

    public ServiceContext(Grid grid, ButtonResult dialogResult = ButtonResult.Yes)
    {
        Set = new Set { Grid = grid };

        storage
            .Setup(m => m.Contents)
            .Returns(Set);

        var updatedEvent = new GridUpdatedEvent();

        updatedEvent.Subscribe(
            action: () => updateCount++,
            keepSubscriberReferenceAlive: true);

        var eventAggregator = new Mock<IEventAggregator>();
        eventAggregator
            .Setup(x => x.GetEvent<SettingsLoadedEvent<Set>>())
            .Returns(loadedEvent);
        eventAggregator
            .Setup(x => x.GetEvent<GridUpdatedEvent>())
            .Returns(updatedEvent);

        var dialogService = new Mock<IDialogService>();
        dialogService.SetReturnsDefault(Task.FromResult(dialogResult));

        var keyListenService = new Mock<IKeyboardService>();
        var dispatcherService = new Mock<IDispatcherService>();

        Service = new Services.GridService.Service(
            eventAggregator: eventAggregator.Object,
            settingsService: storage.Object,
            dialogService: dialogService.Object,
            keyListenService: keyListenService.Object,
            dispatcherService: dispatcherService.Object);

        Reload();
    }

    #endregion Public Constructors

    #region Public Properties

    public Grid Grid => Service.Grid;

    // Number of saves since the settings have been loaded.
    public int SaveCount => storage.Invocations.Count(i => i.Method.Name == "Save") - saveCountAtLoad;

    public Services.GridService.Service Service { get; }

    public Set Set { get; }

    // Number of grid updated events since the settings have been loaded.
    public int UpdateCount => updateCount - updateCountAtLoad;

    #endregion Public Properties

    #region Public Methods

    public void Reload()
    {
        loadedEvent.Publish();

        saveCountAtLoad = storage.Invocations.Count(i => i.Method.Name == "Save");
        updateCountAtLoad = updateCount;
    }

    #endregion Public Methods
}
