using System;
using System.Linq;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Models.Settings;

public class Set
    : IPathHolder
{
    #region Public Properties

    public Grid Grid { get; set; } = new Grid();

    public Options Options { get; set; } = new Options();

    #endregion Public Properties

    #region Public Methods

    public void ConvertPaths(Func<string, string> convert)
    {
        ConvertPaths(Grid, convert);
    }

    #endregion Public Methods

    #region Private Methods

    private static void ConvertPaths(Grid grid, Func<string, string> convert)
    {
        if (grid?.Cells == default)
        {
            return;
        }

        foreach (var cell in grid.Cells)
        {
            if (cell is Player player
                && player.Paths?.Count > 0)
            {
                player.Paths = [.. player.Paths.Select(convert)];
            }
            else if (cell is Group group)
            {
                foreach (var tab in group.Grids)
                {
                    ConvertPaths(tab, convert);
                }
            }
        }
    }

    #endregion Private Methods
}