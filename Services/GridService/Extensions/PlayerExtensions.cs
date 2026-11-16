using SportDJ.Commons.Models.Structure;

namespace SportDJ.Services.GridService.Extensions;

internal static class PlayerExtensions
{
    #region Public Methods

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