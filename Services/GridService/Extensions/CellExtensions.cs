using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions;

internal static class CellExtensions
{
    #region Public Methods

    public static void Move(this Cell cell, OrientationType orientation)
    {
        switch (orientation)
        {
            case OrientationType.Right:

                cell.Column++;

                var moversRight = cell.Grid.Cells
                    .Where(c => c != cell
                        && c.Column == cell.Column
                        && Overlaps(c.Row, c.Rows, cell.Row, cell.Rows)).ToArray();

                foreach (var mover in moversRight)
                {
                    mover.Column--;
                }

                break;

            case OrientationType.Left:

                cell.Column--;

                var moversLeft = cell.Grid.Cells
                    .Where(c => c != cell
                        && c.Column + c.Columns - 1 == cell.Column
                        && Overlaps(c.Row, c.Rows, cell.Row, cell.Rows)).ToArray();

                foreach (var mover in moversLeft)
                {
                    mover.Column++;
                }

                break;

            case OrientationType.Bottom:

                cell.Row++;

                var moversBottom = cell.Grid.Cells
                    .Where(c => c != cell
                        && c.Row == cell.Row
                        && Overlaps(c.Column, c.Columns, cell.Column, cell.Columns)).ToArray();

                foreach (var mover in moversBottom)
                {
                    mover.Row--;
                }

                break;

            case OrientationType.Top:

                cell.Row--;

                var moversTop = cell.Grid.Cells
                    .Where(c => c != cell
                        && c.Row + c.Rows - 1 == cell.Row
                        && Overlaps(c.Column, c.Columns, cell.Column, cell.Columns)).ToArray();

                foreach (var mover in moversTop)
                {
                    mover.Row++;
                }

                break;
        }
    }

    #endregion Public Methods

    #region Private Methods

    // Two half-open ranges [start, start + length) overlap if each one starts before the other one ends.
    // Ranges that merely touch (e.g. [0, 1) and [1, 2)) do not overlap.
    private static bool Overlaps(int startA, int lengthA, int startB, int lengthB)
    {
        return startA < startB + lengthB
            && startB < startA + lengthA;
    }

    #endregion Private Methods
}
