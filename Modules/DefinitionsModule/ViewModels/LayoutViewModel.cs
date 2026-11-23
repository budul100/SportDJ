using System.Threading.Tasks;
using SportDJ.Commons.Enums;
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

    public LayoutViewModel(IGridService gridService)
    {
        this.gridService = gridService;

        AddCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddPlayer(cell, o));

        MoveCellCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveCell(cell, o));

        SwapCommand = new DelegateCommand(
            executeMethod: async () => await SwapAsync());

        AddTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.AddGrid(tab, o));

        MoveTabCommand = new DelegateCommand<OrientationType>(
            executeMethod: o => gridService?.MoveGrid(tab, o));

    }

    #endregion Public Constructors

    #region Public Properties

    public DelegateCommand<OrientationType> AddCellCommand { get; }

    public DelegateCommand<OrientationType> AddTabCommand { get; }

    public bool IsActive => cell != default;

    public bool IsTab => tab != default;

    public DelegateCommand<OrientationType> MoveCellCommand { get; }

    public DelegateCommand<OrientationType> MoveTabCommand { get; }

    public DelegateCommand SwapCommand { get; }

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

    private void Refresh()
    {
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsTab));
    }

    private async Task SwapAsync()
    {
        if (gridService != default
            && cell != default)
        {
            await gridService.SwapAsync(cell);
        }
    }

    #endregion Private Methods
}
