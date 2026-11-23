using System.Linq;
using System.Threading.Tasks;
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

    private const string Titel = "Layout";

    private readonly IGridService gridService;

    private Cell cell;
    private Grid tab;

    #endregion Private Fields

    #region Public Constructors

    public LayoutViewModel(IEventAggregator eventAggregator, IGridService gridService)
    {
        this.gridService = gridService;

        AddCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddPlayer(cell, o));

        MoveCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveCell(cell, o));

        GroupCommand = new DelegateCommand(
            executeMethod: async () => await GroupAsync());

        AddTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddGrid(tab, o));

        MoveTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveGrid(tab, o));

        // The names of the tabs are shown next to the group button
        eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => Refresh(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

    }

    #endregion Public Constructors

    #region Public Properties

    public DelegateCommand<OrientationType> AddCellCommand { get; }

    public DelegateCommand<OrientationType> AddTabCommand { get; }

    public DelegateCommand GroupCommand { get; }

    /// <summary>
    /// Names of the tabs of the group the player belongs to, empty for a player outside of a group.
    /// </summary>
    public string GroupText
    {
        get
        {
            var group = (cell as Player)?.Grid?.Group;

            return group == default
                ? default
                : string.Join(
                    separator: ", ",
                    values: group.Grids.Select(g => g.Name));
        }
    }

    public bool HasGroup => !string.IsNullOrEmpty(GroupText);

    public bool IsActive => cell != default;

    // A group is created from a player, groups are dissolved by removing their tabs
    public bool IsPlayer => cell is Player;

    public bool IsTab => tab != default;

    public DelegateCommand<OrientationType> MoveCellCommand { get; }

    public DelegateCommand<OrientationType> MoveTabCommand { get; }

    public override string Title => Titel;

    #endregion Public Properties

    #region Public Methods

    // Pending assignments are dropped, e.g. if the pane gets closed
    public void Cancel()
    {
        gridService?.CancelListening();
    }

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

    // Drops the targets, which are not part of the grid anymore.
    // The whole grid is rebuilt after each change, so they might not exist anymore.
    public void Validate()
    {
        if (tab != default
            && !tab.IsAttached())
        {
            tab = default;
        }

        if (!cell.IsAttached())
        {
            cell = default;
            tab = default;
        }

        Refresh();
    }

    #endregion Public Methods

    #region Private Methods

    private async Task GroupAsync()
    {
        if (gridService != default
            && cell is Player player)
        {
            await gridService.GroupAsync(player);
        }
    }

    private void Refresh()
    {
        RaisePropertyChanged(nameof(GroupText));
        RaisePropertyChanged(nameof(HasGroup));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsPlayer));
        RaisePropertyChanged(nameof(IsTab));
    }

    #endregion Private Methods
}
