using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using System;
using System.Linq;

namespace SportDJ.Services.GridService.Extensions
{
    internal static class GroupExtensions
    {
        #region Public Methods

        public static Grid Add(this Group group, Grid anchor = default, OrientationType orientation = OrientationType.Right)
        {
            if (orientation != OrientationType.Right
                && orientation != OrientationType.Left)
            {
                throw new ArgumentException(
                    message: "Orientation for tabs must be left or right.",
                    paramName: nameof(orientation));
            }

            var result = group.GetGrid();

            var index = (anchor?.Group.Grids.IndexOf(anchor)) ?? 0;

            if (anchor != default)
            {
                index = orientation == OrientationType.Right
                    ? index + 1
                    : index - 1;
            }

            if (index >= group.Grids.Count)
            {
                group.Grids.Add(
                    item: result);
            }
            else
            {
                group.Grids.Insert(
                    index: index,
                    item: result);
            }

            return result;
        }

        public static void ToPlayer(this Group group)
        {
            if (group?.Grids.Count > 0)
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

        private static Grid GetGrid(this Group group)
        {
            var name = group.GetGridName();

            var result = new Grid
            {
                Name = name,
                Group = group,
            };

            return result;
        }

        private static string GetGridName(this Group group)
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