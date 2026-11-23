using System.Linq;
using SportDJ.Commons.Models.Structure;
using Xunit;

namespace SportDJ.Tests.GridServiceTests;

internal static class TestHelper
{
    #region Public Methods

    public static void AssertNoOverlaps(Grid grid)
    {
        var cells = grid.Cells.ToArray();

        for (var i = 0; i < cells.Length; i++)
        {
            for (var j = i + 1; j < cells.Length; j++)
            {
                var a = cells[i];
                var b = cells[j];

                var overlapping = a.Column < b.Column + b.Columns
                    && b.Column < a.Column + a.Columns
                    && a.Row < b.Row + b.Rows
                    && b.Row < a.Row + a.Rows;

                Assert.False(overlapping, $"Cells overlap: {DescribeCell(a)} and {DescribeCell(b)}");
            }

            if (cells[i] is Group group)
            {
                foreach (var groupGrid in group.Grids)
                {
                    AssertNoOverlaps(groupGrid);
                }
            }
        }
    }

    public static Grid CreateColumn(int count, string prefix = "P")
    {
        return new Grid
        {
            Cells = [.. Enumerable.Range(0, count)
                .Select(i => CreatePlayer($"{prefix}{i}", 0, i))],
        };
    }

    public static Grid CreateGrid(params Cell[] cells)
    {
        return new Grid
        {
            Cells = [.. cells],
        };
    }

    public static Group CreateGroup(int column, int row, params Grid[] tabs)
    {
        var result = new Group
        {
            Column = column,
            Row = row,
        };

        foreach (var tab in tabs)
        {
            result.Grids.Add(tab);
        }

        return result;
    }

    public static Player CreatePlayer(string name, int column, int row)
    {
        return new Player
        {
            Name = name,
            Column = column,
            Row = row,
        };
    }

    public static Grid CreateRow(int count, string prefix = "P")
    {
        return new Grid
        {
            Cells = [.. Enumerable.Range(0, count)
                .Select(i => CreatePlayer($"{prefix}{i}", i, 0))],
        };
    }

    public static Grid CreateTab(string name, params Cell[] cells)
    {
        return new Grid
        {
            Name = name,
            Cells = [.. cells],
        };
    }

    // Complete textual representation of a grid tree to compare states.
    public static string Describe(Grid grid)
    {
        return string.Join(";", grid.Cells.Select(DescribeCell));
    }

    public static Player Find(Grid grid, string name)
    {
        return grid.Cells.OfType<Player>().Single(p => p.Name == name);
    }

    public static string[] Names(Grid grid)
    {
        return [.. grid.Cells.OfType<Player>().Select(p => p.Name)];
    }

    public static (int Column, int Row) Position(Cell cell)
    {
        return (cell.Column, cell.Row);
    }

    public static string[] TabNames(Group group)
    {
        return [.. group.Grids.Select(g => g.Name)];
    }

    #endregion Public Methods

    #region Private Methods

    private static string DescribeCell(Cell cell)
    {
        if (cell is Group group)
        {
            var tabs = group.Grids
                .Select(g => $"{g.Name}{{{Describe(g)}}}");

            return $"[{string.Join("|", tabs)}]@{group.Column},{group.Row}:{group.Columns}x{group.Rows}";
        }

        return $"{(cell as Player)?.Name}@{cell.Column},{cell.Row}";
    }

    #endregion Private Methods
}
