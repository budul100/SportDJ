using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions
{
    internal static class ElementExtensions
    {
        #region Public Methods

        public static Cell AddPlayer(this Grid grid, int column = 0, int row = 0)
        {
            var result = grid.GetPlayer();

            result.Column = column;
            result.Row = row;

            return result;
        }

        public static Player GetPlayer(this Grid grid)
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

        public static void Initialize(this Grid grid)
        {
            if (grid?.Cells?.Any() == true)
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

        public static void Move(this Cell cell, OrientationType orientation)
        {
            switch (orientation)
            {
                case OrientationType.Right:

                    cell.Column++;

                    var moversRight = cell.Grid.Cells
                        .Where(c => c != cell
                            && c.Row <= cell.Row
                            && c.Row + c.Rows >= cell.Row
                            && c.Column == cell.Column).ToArray();

                    foreach (var mover in moversRight)
                    {
                        mover.Column--;
                    }

                    break;

                case OrientationType.Left:

                    cell.Column--;

                    var moversLeft = cell.Grid.Cells
                        .Where(c => c != cell
                            && c.Row <= cell.Row
                            && c.Row + c.Rows >= cell.Row
                            && c.Column + c.Columns - 1 == cell.Column).ToArray();

                    foreach (var mover in moversLeft)
                    {
                        mover.Column++;
                    }

                    break;

                case OrientationType.Bottom:

                    cell.Row++;

                    var moversBottom = cell.Grid.Cells
                        .Where(c => c != cell
                            && c.Column <= cell.Column
                            && c.Column + c.Columns >= cell.Column
                            && c.Row == cell.Row).ToArray();

                    foreach (var mover in moversBottom)
                    {
                        mover.Row--;
                    }

                    break;

                case OrientationType.Top:

                    cell.Row--;

                    var moversTop = cell.Grid.Cells
                        .Where(c => c != cell
                            && c.Column <= cell.Column
                            && c.Column + c.Columns >= cell.Column
                            && c.Row + c.Rows - 1 == cell.Row).ToArray();

                    foreach (var mover in moversTop)
                    {
                        mover.Row++;
                    }

                    break;
            }
        }

        public static void Remove(this Cell cell)
        {
            cell.Grid.Cells.Remove(cell);
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

        public static void ToGroup(this Player player)
        {
            var group = new Group
            {
                Column = player.Column,
                Row = player.Row,
                Grid = player.Grid,
            };

            player.Grid.Cells.Add(group);

            var tab = AddTab(group);

            tab.Cells.Add(player);
            player.Grid.Cells.Remove(player);

            player.Grid = tab;
        }

        public static void ToPlayer(this Group group)
        {
            if (group?.Grids.Any() == true)
            {
                var cells = group.Grids
                    .SelectMany(g => g.Cells)
                    .Where(c => c != default).ToArray();

                foreach (var cell in cells)
                {
                    cell.Column = group.Column;
                    cell.Row = group.Row;
                    cell.Grid = group.Grid;

                    group.Grid.Cells.Add(cell);
                    group.Grid.Cells.Remove(group);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static Grid AddTab(this Group group)
        {
            var name = group.GetTabName();

            var result = new Grid
            {
                Name = name,
            };

            group.Grids.Add(result);

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

        private static string GetTabName(this Group group)
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

        #endregion Private Methods
    }
}