using System;
using System.Collections.Generic;
using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions;

internal static class GridExtensions
{
    #region Public Methods

    public static Cell Add(this Grid grid, Cell anchor = default, OrientationType orientation = OrientationType.Right)
    {
        var result = grid.GetPlayer();

        result.Column = anchor?.Column ?? 0;
        result.Row = anchor?.Row ?? 0;

        if (anchor != default)
        {
            result.Move(orientation);
        }

        return result;
    }

    // Checks if the group is part of the tab or of one of its nested tabs
    public static bool Contains(this Grid tab, Group group)
    {
        var grid = group?.Grid;

        while (grid != default)
        {
            if (grid == tab)
            {
                return true;
            }

            grid = grid.Group?.Grid;
        }

        return false;
    }

    // Returns all tabs of the groups of the grid, including the tabs of nested groups
    public static IEnumerable<Grid> GetTabs(this Grid grid)
    {
        if (grid?.Cells != default)
        {
            foreach (var group in grid.Cells.OfType<Group>())
            {
                foreach (var tab in group.Grids)
                {
                    yield return tab;

                    foreach (var nested in tab.GetTabs())
                    {
                        yield return nested;
                    }
                }
            }
        }
    }

    public static void Initialize(this Grid grid)
    {
        if (grid?.Cells?.Count > 0)
        {
            foreach (var cell in grid.Cells)
            {
                cell.Grid = grid;

                if (cell is Group group)
                {
                    foreach (var groupGrid in group.Grids)
                    {
                        groupGrid.Group = group;

                        groupGrid.Initialize();
                    }
                }
            }
        }
    }

    // Returns the maximum number of columns, rows and tabs found in the grid and all of its nested tabs.
    // Expects a grid that has been tidied before, as it relies on the Columns and Rows of the grids.
    public static (int Columns, int Rows, int Tabs) Measure(this Grid grid)
    {
        var columns = grid.Columns;
        var rows = grid.Rows;
        var tabs = 0;

        foreach (var group in grid.Cells.OfType<Group>())
        {
            tabs = Math.Max(tabs, group.Grids.Count);

            foreach (var groupGrid in group.Grids)
            {
                var (Columns, Rows, Tabs) = groupGrid.Measure();

                columns = Math.Max(columns, Columns);
                rows = Math.Max(rows, Rows);
                tabs = Math.Max(tabs, Tabs);
            }
        }

        return (columns, rows, tabs);
    }

    public static void Move(this Grid grid, OrientationType orientation)
    {
        if (orientation != OrientationType.Right
            && orientation != OrientationType.Left)
        {
            throw new ArgumentException(
                message: "Orientation for tabs must be left or right.",
                paramName: nameof(orientation));
        }

        var index = grid.Group.Grids.IndexOf(grid);

        if (orientation == OrientationType.Right
            && index < grid.Group.Grids.Count - 1)
        {
            var temp = grid.Group.Grids[index];

            grid.Group.Grids.RemoveAt(index);
            grid.Group.Grids.Insert(
                index: index + 1,
                item: temp);
        }
        else if (orientation == OrientationType.Left
            && index > 0)
        {
            var temp = grid.Group.Grids[index];

            grid.Group.Grids.RemoveAt(index);
            grid.Group.Grids.Insert(
                index: index - 1,
                item: temp);
        }
    }

    /// <summary>
    /// Moves the tab out of its group into a new group of its own, to the right of the old group.
    /// Overlaps with other cells are resolved when the grid is tidied.
    /// </summary>
    public static void MoveOut(this Grid tab)
    {
        var source = tab?.Group;
        var parent = source?.Grid;

        if (parent != default
            && source.Grids.Count > 1)
        {
            source.Grids.Remove(tab);

            var group = new Group
            {
                Column = source.Column + source.Columns,
                Row = source.Row,
                Grid = parent,
            };

            group.Grids.Add(tab);

            tab.Group = group;

            parent.Cells.Add(group);
        }
    }

    /// <summary>
    /// Moves the tab to the end of another group. The old group disappears together with its last tab.
    /// </summary>
    public static void MoveToGroup(this Grid tab, Group target)
    {
        var source = tab?.Group;

        if (source != default
            && target != default
            && source != target)
        {
            source.Grids.Remove(tab);

            target.Grids.Add(tab);

            tab.Group = target;

            if (source.Grids.Count == 0)
            {
                source.Grid.Cells.Remove(source);
            }
        }
    }

    /// <summary>
    /// Removes the tab together with its cells. The group disappears together with its last tab.
    /// </summary>
    public static void Remove(this Grid tab)
    {
        var group = tab?.Group;
        var parent = group?.Grid;

        if (parent != default)
        {
            group.Grids.Remove(tab);

            if (group.Grids.Count == 0)
            {
                parent.Cells.Remove(group);
            }
        }
    }

    public static void Tidy(this Grid grid)
    {
        if (grid != default)
        {
            if ((grid.Cells?.Count ?? 0) == 0)
            {
                grid.Add();
            }

            var groups = grid.Cells
                .OfType<Group>().ToArray();

            foreach (var group in groups)
            {
                if ((group.Grids?.Count ?? 0) == 0)
                {
                    group.Add();
                }

                foreach (var groupGrid in group.Grids)
                {
                    groupGrid.Tidy();
                }

                group.Columns = (group.Grids?.Count > 0) ? group.Grids.Max(g => g.Columns) : 0;
                group.Rows = (group.Grids?.Count > 0) ? group.Grids.Max(g => g.Rows) : 0;
            }

            grid.ResolveOverlaps();

            var rowMin = grid.Cells.Min(c => c.Row);

            if (rowMin != 0)
            {
                foreach (var cell in grid.Cells)
                {
                    cell.Row -= rowMin;
                }
            }

            var columnMin = grid.Cells.Min(c => c.Column);

            if (columnMin != 0)
            {
                foreach (var cell in grid.Cells)
                {
                    cell.Column -= columnMin;
                }
            }

            grid.Cells = [.. grid.Cells
                .OrderBy(c => c.Row)
                .ThenBy(c => c.Column)];

            grid.Columns = grid.Cells.Max(c => c.Column + c.Columns);
            grid.Rows = grid.Cells.Max(c => c.Row + c.Rows);
        }
    }

    #endregion Public Methods

    #region Private Methods

    private static Player GetPlayer(this Grid grid)
    {
        var name = grid.GetPlayerName();

        var result = new Player
        {
            Name = name,
            Grid = grid,
        };

        grid.Cells.Add(result);

        return result;
    }

    private static string GetPlayerName(this Grid grid)
    {
        var result = default(string);

        var relevants = grid.Cells.OfType<Player>()
            .Select(p => p.Name).ToArray();

        var number = 0;

        do
        {
            result = $"Player {++number}";
        } while (relevants.Contains(result));

        return result;
    }

    // Pushes overlapping cells to the right of the cell they overlap. Of two overlapping cells the one that comes
    // later (by row, then column) is pushed, cells starting at the same position are pushed in list order.
    // As a pushed cell can land on a cell that has already been handled, this is repeated until the layout is stable.
    private static void ResolveOverlaps(this Grid grid)
    {
        var cells = grid.Cells.ToArray();

        var maxPasses = (cells.Length * cells.Length) + 1;
        var moved = true;

        for (var pass = 0; moved && pass < maxPasses; pass++)
        {
            moved = false;

            foreach (var cell in cells)
            {
                // Only the cell itself is compared, so pushing another cell does not change the checks for this cell
                foreach (var inner in cells)
                {
                    if (inner != cell
                        && inner.Column < cell.Column + cell.Columns
                        && cell.Column < inner.Column + inner.Columns
                        && inner.Row < cell.Row + cell.Rows
                        && cell.Row < inner.Row + inner.Rows
                        && (inner.Row > cell.Row
                            || (inner.Row == cell.Row && inner.Column >= cell.Column)))
                    {
                        inner.Column = cell.Column + cell.Columns;

                        moved = true;
                    }
                }
            }
        }
    }

    #endregion Private Methods
}
