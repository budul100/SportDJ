using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class RemoveTests
{
    #region Public Methods

    [Fact]
    public async Task RemoveCellAsync_Confirmed_RemovesCellAndSaves()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);
        var context = new ServiceContext(CreateGrid(a, b));

        await context.Service.RemoveCellAsync(a);

        Assert.Equal(["B"], Names(context.Grid));
        Assert.Equal((0, 0), Position(b));
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public async Task RemoveCellAsync_LastCell_LeavesFreshPlayer()
    {
        var a = CreatePlayer("A", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        await context.Service.RemoveCellAsync(a);

        Assert.Equal(["Player 1"], Names(context.Grid));
    }

    [Fact]
    public async Task RemoveCellAsync_Null_DoesNothing()
    {
        var context = new ServiceContext(CreateGrid(CreatePlayer("A", 0, 0)));

        await context.Service.RemoveCellAsync(null);

        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task RemoveCellAsync_Rejected_KeepsCell()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);
        var context = new ServiceContext(CreateGrid(a, b), ButtonResult.No);

        await context.Service.RemoveCellAsync(a);

        Assert.Equal(["A", "B"], Names(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task RemoveGridAsync_Confirmed_RemovesTabWithItsPlayers()
    {
        var group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", CreatePlayer("A", 0, 0)),
            CreateTab("Tab 2", CreatePlayer("B", 0, 0)));

        var context = new ServiceContext(CreateGrid(group));

        await context.Service.RemoveGridAsync(group.Grids[0]);

        Assert.Equal(["Tab 2"], TabNames(group));
        Assert.Empty(context.Grid.Cells.OfType<Player>());
        Assert.Same(group, context.Grid.Cells.Single());
        Assert.Equal(1, context.SaveCount);

        AssertNoOverlaps(context.Grid);
    }

    [Fact]
    public async Task RemoveGridAsync_GridNotInGroup_DoesNotThrow()
    {
        var group = CreateGroup(0, 0, CreateTab("Tab 1", CreatePlayer("A", 0, 0)));
        var context = new ServiceContext(CreateGrid(group));

        var foreign = new Grid
        {
            Name = "Foreign",
            Group = group,
        };

        await context.Service.RemoveGridAsync(foreign);

        Assert.Equal(["Tab 1"], TabNames(group));
    }

    [Fact]
    public async Task RemoveGridAsync_LastTab_RemovesGroupWithItsPlayers()
    {
        var group = CreateGroup(
            1,
            0,
            CreateTab("Tab 1", CreatePlayer("PA", 0, 0), CreatePlayer("PB", 1, 0)));

        var context = new ServiceContext(CreateGrid(CreatePlayer("P0", 0, 0), group));

        await context.Service.RemoveGridAsync(group.Grids[0]);

        Assert.Empty(context.Grid.Cells.OfType<Group>());
        Assert.Equal(["P0"], Names(context.Grid));
        Assert.All(context.Grid.Cells, c => Assert.Same(context.Grid, c.Grid));

        AssertNoOverlaps(context.Grid);
    }

    [Fact]
    public async Task RemoveGridAsync_Rejected_KeepsTab()
    {
        var group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", CreatePlayer("A", 0, 0)),
            CreateTab("Tab 2", CreatePlayer("B", 0, 0)));

        var context = new ServiceContext(CreateGrid(group), ButtonResult.No);

        await context.Service.RemoveGridAsync(group.Grids[0]);

        Assert.Equal(2, group.Grids.Count);
        Assert.Equal(0, context.SaveCount);
    }

    #endregion Public Methods
}
