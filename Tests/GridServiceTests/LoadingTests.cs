using System.Linq;
using SportDJ.Commons.Models.Structure;
using Xunit;

namespace SportDJ.Tests.GridServiceTests;

public class LoadingTests
{
    #region Public Methods

    [Fact]
    public void LoadEmptyGroup()
    {
        var cell = new Group
        {
            Column = 0,
            Row = 0
        };

        var context = new ServiceContext(new Grid { Cells = [cell] });

        Assert.True((context.Grid.Cells.Single() as Group).Grids.Single().Cells.Single() is Player);
    }

    [Fact]
    public void LoadEmptySet()
    {
        var context = new ServiceContext(new Grid());

        Assert.True(context.Grid.Cells.Single() is Player);
    }

    #endregion Public Methods
}
