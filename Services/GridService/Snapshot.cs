using System.Collections.Generic;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService;

/// <summary>
/// Captures the complete state of a grid tree (cell positions, parent references, cell and tab lists)
/// so that a failed or rejected operation can be undone.
/// </summary>
internal sealed class Snapshot
{
    #region Private Fields

    private readonly List<(Cell Cell, int Column, int Row, Grid Grid)> cells = [];
    private readonly List<(Grid Grid, Cell[] Cells, Group Group, int Columns, int Rows)> grids = [];
    private readonly List<(Group Group, Grid[] Grids, int Columns, int Rows)> groups = [];

    #endregion Private Fields

    #region Public Constructors

    public Snapshot(Grid grid)
    {
        Capture(grid);
    }

    #endregion Public Constructors

    #region Public Methods

    public void Restore()
    {
        foreach (var entry in cells)
        {
            entry.Cell.Column = entry.Column;
            entry.Cell.Row = entry.Row;
            entry.Cell.Grid = entry.Grid;
        }

        foreach (var entry in grids)
        {
            entry.Grid.Cells = [.. entry.Cells];
            entry.Grid.Group = entry.Group;
            entry.Grid.Columns = entry.Columns;
            entry.Grid.Rows = entry.Rows;
        }

        foreach (var entry in groups)
        {
            entry.Group.Columns = entry.Columns;
            entry.Group.Rows = entry.Rows;

            entry.Group.Grids.Clear();

            foreach (var grid in entry.Grids)
            {
                entry.Group.Grids.Add(grid);
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void Capture(Grid grid)
    {
        var gridCells = grid.Cells.ToArray();

        grids.Add((grid, gridCells, grid.Group, grid.Columns, grid.Rows));

        foreach (var cell in gridCells)
        {
            cells.Add((cell, cell.Column, cell.Row, cell.Grid));

            if (cell is Group group)
            {
                var groupGrids = group.Grids.ToArray();

                groups.Add((group, groupGrids, group.Columns, group.Rows));

                foreach (var groupGrid in groupGrids)
                {
                    Capture(groupGrid);
                }
            }
        }
    }

    #endregion Private Methods
}
