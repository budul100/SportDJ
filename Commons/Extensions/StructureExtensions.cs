using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Extensions;

public static class StructureExtensions
{
    #region Public Methods

    /// <summary>
    /// Checks if the cell is still part of the grid tree, including all of its parent groups and tabs.
    /// A removed cell keeps its references, so they alone do not tell.
    /// </summary>
    public static bool IsAttached(this Cell cell)
    {
        var grid = cell?.Grid;

        if (grid?.Cells?.Contains(cell) != true)
        {
            return false;
        }

        var group = grid.Group;

        // The root grid has no group
        return group == default
            || (group.Grids.Contains(grid) && group.IsAttached());
    }

    #endregion Public Methods
}
