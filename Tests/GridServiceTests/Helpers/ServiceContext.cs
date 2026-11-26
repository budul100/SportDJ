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

    private readonly SettingsLoadedEvent<Set> loadedEvent;
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

        // A real aggregator, so that every event the service publishes (added, moved, updated, ...) is available
        var eventAggregator = new EventAggregator();

        loadedEvent = eventAggregator.GetEvent<SettingsLoadedEvent<Set>>();

        eventAggregator.GetEvent<GridUpdatedEvent>().Subscribe(
            action: () => updateCount++,
            keepSubscriberReferenceAlive: true);

        var dialogService = new Mock<IDialogService>();
        dialogService.SetReturnsDefault(Task.FromResult(dialogResult));

        var keyListenService = new Mock<IKeyboardService>();
        var dispatcherService = new Mock<IDispatcherService>();
        var controllerService = new Mock<IControllerService>();

        Service = new Services.GridService.Service(
            eventAggregator: eventAggregator,
            settingsService: storage.Object,
            dialogService: dialogService.Object,
            keyListenService: keyListenService.Object,
            dispatcherService: dispatcherService.Object,
            controllerService: controllerService.Object);

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
