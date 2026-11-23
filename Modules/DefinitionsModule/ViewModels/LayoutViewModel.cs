using System.Threading.Tasks;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class LayoutViewModel
    : TabViewModel
{
    #region Private Fields

    private const string KeyboardHint = "Press a key...";
    private const string NoKey = "None";
    private const string Titel = "Layout";

    private readonly IGridService gridService;

    private Cell cell;
    private Grid tab;

    #endregion Private Fields

    #region Public Constructors

    public LayoutViewModel(IEventAggregator eventAggregator, IGridService gridService,
        IKeyboardService keyListenService)
    {
        this.gridService = gridService;

        AddCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddPlayer(cell, o));

        MoveCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveCell(cell, o));

        SwapCommand = new DelegateCommand(
            executeMethod: async () => await SwapAsync());

        RemoveCellCommand = new DelegateCommand(
            executeMethod: async () => await RemoveCellAsync());

        AddTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddGrid(tab, o));

        MoveTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveGrid(tab, o));

        RemoveTabCommand = new DelegateCommand(
            executeMethod: async () => await RemoveTabAsync());

        ListenKeyCommand = new DelegateCommand(ToggleKey);
        ClearKeyCommand = new DelegateCommand(ClearKey);

        // The whole grid is rebuilt after each change, so the targets might not exist anymore
        eventAggregator?.GetEvent<GridUpdatedEvent>().Subscribe(
            action: Validate,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => Refresh(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: g => tab != default
                && tab == g);

        if (keyListenService != default)
        {
            keyListenService.Changed += (_, _) => Refresh();
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public static int MaxLengthName => Constants.NameMaxLength;

    public DelegateCommand<OrientationType> AddCellCommand { get; }

    public DelegateCommand<OrientationType> AddTabCommand { get; }

    public DelegateCommand ClearKeyCommand { get; }

    public bool IsActive => cell != default;

    public bool IsListeningKey => gridService?.IsListening(tab) == true;

    public bool IsTab => tab != default;

    public string KeyText => IsListeningKey
        ? KeyboardHint
        : tab?.KeyCode.GetKey() ?? NoKey;

    public DelegateCommand ListenKeyCommand { get; }

    public DelegateCommand<OrientationType> MoveCellCommand { get; }

    public DelegateCommand<OrientationType> MoveTabCommand { get; }

    public DelegateCommand RemoveCellCommand { get; }

    public DelegateCommand RemoveTabCommand { get; }

    public DelegateCommand SwapCommand { get; }

    public string TabName
    {
        get => tab?.Name;

        set => gridService?.SetName(tab, value);
    }

    public override string Title => Titel;

    #endregion Public Properties

    #region Public Methods

    public void SetCell(Cell cell)
    {
        if (cell != default)
        {
            gridService?.CancelListening();

            this.cell = cell;
            tab = default;

            Refresh();
        }
    }

    public void SetTab(Grid tab)
    {
        if (tab?.Group != default)
        {
            gridService?.CancelListening();

            this.tab = tab;
            cell = tab.Group;

            Refresh();
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void ClearKey()
    {
        gridService?.SetKeyCode(tab, default);
    }

    private void Refresh()
    {
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsListeningKey));
        RaisePropertyChanged(nameof(IsTab));
        RaisePropertyChanged(nameof(KeyText));
        RaisePropertyChanged(nameof(TabName));
    }

    private async Task RemoveCellAsync()
    {
        if (gridService != default
            && cell != default)
        {
            await gridService.RemoveCellAsync(cell);

            Validate();
        }
    }

    private async Task RemoveTabAsync()
    {
        if (gridService != default
            && tab != default)
        {
            await gridService.RemoveGridAsync(tab);

            Validate();
        }
    }

    private async Task SwapAsync()
    {
        if (gridService != default
            && cell != default)
        {
            await gridService.SwapAsync(cell);

            Validate();
        }
    }

    private void ToggleKey()
    {
        if (gridService?.IsListening(tab) == true)
        {
            gridService.CancelListening();
        }
        else
        {
            gridService?.ListenKey(tab);
        }
    }

    // Drops the targets, which are not part of the grid anymore
    private void Validate()
    {
        var isCellAttached = cell?.Grid?.Cells?.Contains(cell) == true;
        var isTabAttached = tab?.Group?.Grids?.Contains(tab) == true;

        if (!isTabAttached)
        {
            tab = default;
        }

        if (!isCellAttached)
        {
            cell = default;
            tab = default;
        }

        Refresh();
    }

    #endregion Private Methods
}
