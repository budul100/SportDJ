using System;
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

            foreach (var cell in grid.Cells)
            {
                var innerColumns = grid.Cells
                    .Where(c => c != cell
                        && c.Column >= cell.Column
                        && c.Column < cell.Column + cell.Columns
                        && c.Row < cell.Row + cell.Rows
                        && c.Row >= cell.Row).ToArray();

                foreach (var innerColumn in innerColumns)
                {
                    innerColumn.Column = cell.Column + cell.Columns;
                }

                var innerRows = grid.Cells
                    .Where(c => c != cell
                        && c.Row >= cell.Row
                        && c.Row < cell.Row + cell.Rows
                        && c.Column < cell.Column + cell.Columns
                        && c.Column >= cell.Column).ToArray();

                foreach (var innerRow in innerRows)
                {
                    innerRow.Row = cell.Row + cell.Rows;
                }
            }

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

    #endregion Private Methods
}