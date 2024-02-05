using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions
{
    internal static class ElementExtensions
    {
        #region Public Methods

        public static int GetColumn(this Cell cell, OrientationType orientation)
        {
            var result = cell.Column;

            if (orientation == OrientationType.Right)
            {
                result++;
            }
            else if (orientation == OrientationType.Left)
            {
                result--;
            }

            return result;
        }

        public static Player GetPlayer(this Grid grid)
        {
            var name = grid.GetPlayerName();

            var result = new Player
            {
                Name = name,
            };

            grid.Cells.Add(result);

            return result;
        }

        public static int GetRow(this Cell cell, OrientationType orientation)
        {
            var result = cell.Row;

            if (orientation == OrientationType.Bottom)
            {
                result++;
            }
            else if (orientation == OrientationType.Top)
            {
                result--;
            }

            return result;
        }

        public static string GetTabName(this Group group)
        {
            var result = default(string);

            var relevants = group.Grids
                .Select(p => p.Name).ToArray();

            var number = 0;

            do
            {
                result = $"Tab {++number}";
            } while (relevants.Contains(result));

            return result;
        }

        public static void Move(this Grid grid, OrientationType orientation, int column, int row)
        {
            switch (orientation)
            {
                case OrientationType.Right:

                    var moversRight = grid.Cells
                        .Where(c => c.Row == row
                            && c.Column >= column)
                        .OrderBy(c => c.Column).ToArray();

                    foreach (var mover in moversRight)
                    {
                        if (mover.Column > column++)
                        {
                            break;
                        }

                        mover.Column++;
                    }

                    break;

                case OrientationType.Left:

                    var moversLeft = grid.Cells
                        .Where(c => c.Row == row
                            && c.Column <= column)
                        .OrderByDescending(c => c.Column).ToArray();

                    foreach (var mover in moversLeft)
                    {
                        if (mover.Column < column--)
                        {
                            break;
                        }

                        mover.Column--;
                    }

                    break;

                case OrientationType.Bottom:

                    var moversBottom = grid.Cells
                        .Where(c => c.Column == column
                            && c.Row >= row)
                        .OrderBy(c => c.Row).ToArray();

                    foreach (var mover in moversBottom)
                    {
                        if (mover.Row > row++)
                        {
                            break;
                        }

                        mover.Row++;
                    }

                    break;

                case OrientationType.Top:

                    var moversTop = grid.Cells
                        .Where(c => c.Column == column
                            && c.Row <= row)
                        .OrderByDescending(c => c.Row).ToArray();

                    foreach (var mover in moversTop)
                    {
                        if (mover.Row < row--)
                        {
                            break;
                        }

                        mover.Row--;
                    }

                    break;
            }
        }

        public static void Tidy(this Grid grid)
        {
            if (grid != default)
            {
                if (grid.Cells?.Any() != true)
                {
                    grid.GetPlayer();
                }
                else
                {
                    var groups = grid.Cells
                        .OfType<Group>().ToArray();

                    foreach (var group in groups)
                    {
                        foreach (var groupGrid in group.Grids)
                        {
                            groupGrid.Tidy();
                        }

                        group.Columns = group.Grids.Max(g => g.Columns);
                        group.Rows = group.Grids.Max(g => g.Rows);
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
                }

                grid.Columns = grid.Cells.Max(c => c.Column + c.Columns);
                grid.Rows = grid.Cells.Max(c => c.Row + c.Rows);
            }
        }

        #endregion Public Methods

        #region Private Methods

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
}