using System.Linq;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions;

internal static class PlayerExtensions
{
    #region Public Methods

    /// <summary>
    /// Puts the player into a new tab at the end of the group.
    /// The old tab of the player is dissolved, if it has no other cells.
    /// </summary>
    public static Grid MoveToGroup(this Player player, Group group)
    {
        var source = player.Grid;

        var tab = group.Add(
            anchor: group.Grids.LastOrDefault());

        source.Cells.Remove(player);

        player.Column = 0;
        player.Row = 0;
        player.Grid = tab;

        tab.Cells.Add(player);

        if (source.Cells.Count == 0
            && source.Group != default)
        {
            source.Remove();
        }

        return tab;
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

        var tab = group.Add();

        tab.Cells.Add(player);
        player.Grid.Cells.Remove(player);

        player.Grid = tab;
    }

    #endregion Public Methods
}
