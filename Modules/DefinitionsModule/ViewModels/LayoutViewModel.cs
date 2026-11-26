using System.Collections.Generic;
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
    private GroupOption selectedTarget;
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

        RemoveCellCommand = new DelegateCommand(
            executeMethod: async () => await RemoveCellAsync());

        RemoveTabCommand = new DelegateCommand(
            executeMethod: async () => await RemoveTabAsync());

        MoveIntoGroupCommand = new DelegateCommand(MoveIntoGroup);

        MoveOutOfGroupCommand = new DelegateCommand(
            executeMethod: () => gridService?.MoveOutOfGroup(tab));

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

    // A player, which is the only cell of its grid, cannot leave it
    public bool CanMoveIntoGroup => SelectedTarget != default
        && (tab != default
            || (cell is Player player && player.Grid?.Cells?.Count > 1));

    // The last tab of a group cannot be removed, the group would be empty
    // A tab can only leave its group if the group keeps another tab
    public bool CanMoveOutOfGroup => tab?.Group?.Grids?.Count > 1;

    public bool CanRemoveTab => tab?.Group?.Grids?.Count > 1;

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

    public DelegateCommand MoveIntoGroupCommand { get; }

    public DelegateCommand MoveOutOfGroupCommand { get; }

    public DelegateCommand<OrientationType> MoveTabCommand { get; }

    // Removes the player, only for players
    public DelegateCommand RemoveCellCommand { get; }

    // Removes the tab with its players, only for tabs
    public DelegateCommand RemoveTabCommand { get; }

    public GroupOption SelectedTarget
    {
        get => selectedTarget;

        set
        {
            if (SetProperty(ref selectedTarget, value))
            {
                RaisePropertyChanged(nameof(CanMoveIntoGroup));
            }
        }
    }

    // The groups, which the player or tab can be moved into
    public GroupOption[] Targets { get; private set; } = [];

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
        else if (tab != default)
        {
            // The tab might have been moved into another group
            cell = tab.Group;
        }

        Refresh();
    }

    #endregion Public Methods

    #region Private Methods

    private static void CollectGroups(Grid grid, Grid excluded, Group own, List<GroupOption> result)
    {
        foreach (var group in grid.Cells.OfType<Group>())
        {
            if (group != own)
            {
                var text = string.Join(
                    separator: ", ",
                    values: group.Grids.Select(g => g.Name));

                // Groups with the same tabs are numbered, so they can be told apart
                var count = result.Count(o => o.Text == text || o.Text.StartsWith($"{text} ("));

                result.Add(new GroupOption(
                    Group: group,
                    Text: count > 0
                        ? $"{text} ({count + 1})"
                        : text));
            }

            foreach (var groupTab in group.Grids.Where(g => g != excluded))
            {
                CollectGroups(
                    grid: groupTab,
                    excluded: excluded,
                    own: own,
                    result: result);
            }
        }
    }

    private async Task GroupAsync()
    {
        if (gridService != default
            && cell is Player player)
        {
            await gridService.GroupAsync(player);
        }
    }

    private void MoveIntoGroup()
    {
        var group = SelectedTarget?.Group;

        if (group != default)
        {
            if (tab != default)
            {
                gridService?.MoveIntoGroup(tab, group);
            }
            else if (cell is Player player)
            {
                gridService?.MoveIntoGroup(player, group);
            }
        }
    }

    private void Refresh()
    {
        RefreshTargets();

        RaisePropertyChanged(nameof(CanMoveIntoGroup));
        RaisePropertyChanged(nameof(CanMoveOutOfGroup));
        RaisePropertyChanged(nameof(CanRemoveTab));
        RaisePropertyChanged(nameof(GroupText));
        RaisePropertyChanged(nameof(HasGroup));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsPlayer));
        RaisePropertyChanged(nameof(IsTab));
    }

    private void RefreshTargets()
    {
        var result = new List<GroupOption>();

        if (gridService?.Grid != default
            && cell != default)
        {
            // A tab neither moves into its own group nor into a group, which it contains itself
            CollectGroups(
                grid: gridService.Grid,
                excluded: tab,
                own: tab?.Group,
                result: result);
        }

        Targets = [.. result];

        // The selection stays, if its group is still a target
        selectedTarget = Targets
            .FirstOrDefault(t => t.Group == selectedTarget?.Group)
            ?? Targets.FirstOrDefault();

        RaisePropertyChanged(nameof(Targets));
        RaisePropertyChanged(nameof(SelectedTarget));
    }

    private async Task RemoveCellAsync()
    {
        // The pane closes itself, as soon as the grid reports the removal
        if (gridService != default
            && cell is Player player)
        {
            await gridService.RemoveCellAsync(player);
        }
    }

    private async Task RemoveTabAsync()
    {
        if (gridService != default
            && tab != default)
        {
            await gridService.RemoveGridAsync(tab);
        }
    }

    #endregion Private Methods
}
