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

    private const string NewGroupText = "New group";
    private const string Titel = "Layout";

    private readonly IGridService gridService;

    private Cell cell;
    private bool resetPending;
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

        ConfirmGroupCommand = new DelegateCommand(
            executeMethod: async () => await ConfirmGroupAsync());

        AddTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddGrid(tab, o));

        MoveTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveGrid(tab, o));

        RemoveCellCommand = new DelegateCommand(
            executeMethod: async () => await RemoveCellAsync());

        RemoveTabCommand = new DelegateCommand(
            executeMethod: async () => await RemoveTabAsync());

        // The names of the tabs are shown in the selection of the groups
        eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => Refresh(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);
    }

    #endregion Public Constructors

    #region Public Properties

    public DelegateCommand<OrientationType> AddCellCommand { get; }

    public DelegateCommand<OrientationType> AddTabCommand { get; }

    // New group or move into the selected group is only offered if it changes something
    public bool CanConfirmGroup
    {
        get
        {
            if (selectedTarget == default)
            {
                return false;
            }

            if (tab != default)
            {
                // A tab, which is alone in its group, is in a group of its own already
                return selectedTarget.IsNew
                    ? tab.Group?.Grids?.Count > 1
                    : selectedTarget.Group != tab.Group;
            }

            if (cell is Player player)
            {
                // The only player of the root grid cannot leave it, the grid would be empty
                return selectedTarget.IsNew
                    || (selectedTarget.Group != player.Grid?.Group
                        && (player.Grid?.Cells?.Count > 1 || player.Grid?.Group != default));
            }

            return false;
        }
    }

    public DelegateCommand ConfirmGroupCommand { get; }

    public bool IsActive => cell != default;

    // A group is created from a player, groups are dissolved by removing their tabs
    public bool IsPlayer => cell is Player;

    public bool IsTab => tab != default;

    public DelegateCommand<OrientationType> MoveCellCommand { get; }

    public DelegateCommand<OrientationType> MoveTabCommand { get; }

    // Removes the player, only for players
    public DelegateCommand RemoveCellCommand { get; }

    // Removes the tab with all of its players, only for tabs
    public DelegateCommand RemoveTabCommand { get; }

    public GroupOption SelectedTarget
    {
        get => selectedTarget;

        set
        {
            // The selection control resets its selection, while the items are exchanged
            if ((value != default || Targets.Length == 0)
                && SetProperty(ref selectedTarget, value))
            {
                RaisePropertyChanged(nameof(CanConfirmGroup));
            }
        }
    }

    // The groups in the tree and as last entry the new group
    public GroupOption[] Targets { get; private set; } = [];

    public override string Title => Titel;

    #endregion Public Properties

    #region Private Properties

    // The group of the tab or of the tab of the player, empty for a player outside of a group
    private Group CurrentGroup => tab != default
        ? tab.Group
        : (cell as Player)?.Grid?.Group;

    #endregion Private Properties

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

            Refresh(reset: true);
        }
    }

    public void SetTab(Grid tab)
    {
        if (tab?.Group != default)
        {
            gridService?.CancelListening();

            this.tab = tab;
            cell = tab.Group;

            Refresh(reset: true);
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

    private static void CollectGroups(Grid grid, Grid excluded, List<GroupOption> result)
    {
        foreach (var group in grid.Cells.OfType<Group>())
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

            foreach (var groupTab in group.Grids.Where(g => g != excluded))
            {
                CollectGroups(
                    grid: groupTab,
                    excluded: excluded,
                    result: result);
            }
        }
    }

    private async Task ConfirmGroupAsync()
    {
        var target = selectedTarget;

        if (gridService != default
            && target != default)
        {
            // The selection shows the current group again, as soon as the grid reports the change
            resetPending = true;

            if (tab != default)
            {
                if (target.IsNew)
                {
                    gridService.MoveOutOfGroup(tab);
                }
                else
                {
                    gridService.MoveIntoGroup(tab, target.Group);
                }
            }
            else if (cell is Player player)
            {
                if (target.IsNew)
                {
                    await gridService.GroupAsync(player);
                }
                else
                {
                    gridService.MoveIntoGroup(player, target.Group);
                }
            }
        }
    }

    private void Refresh(bool reset = false)
    {
        RefreshTargets(reset || resetPending);

        resetPending = false;

        RaisePropertyChanged(nameof(CanConfirmGroup));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsPlayer));
        RaisePropertyChanged(nameof(IsTab));
    }

    private void RefreshTargets(bool reset)
    {
        var result = new List<GroupOption>();

        if (gridService?.Grid != default
            && cell != default)
        {
            // A tab does not move into a group, which it contains itself
            CollectGroups(
                grid: gridService.Grid,
                excluded: tab,
                result: result);

            result.Add(new GroupOption(
                Group: default,
                Text: NewGroupText));
        }

        var targets = result.ToArray();

        var selection = default(GroupOption);

        if (!reset
            && selectedTarget != default)
        {
            selection = targets
                .FirstOrDefault(t => t.IsNew == selectedTarget.IsNew
                    && t.Group == selectedTarget.Group);
        }

        // Without a choice the current group is shown, outside of a group the new one
        selection ??= targets
            .FirstOrDefault(t => !t.IsNew
                && t.Group == CurrentGroup)
            ?? targets.LastOrDefault();

        Targets = targets;

        RaisePropertyChanged(nameof(Targets));

        // The control has reset its selection by the new items
        selectedTarget = selection;

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
