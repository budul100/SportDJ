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

    #endregion Public Methods
}