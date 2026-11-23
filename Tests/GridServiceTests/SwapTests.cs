using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class SwapTests
{
    #region Public Methods

    [Fact]
    public async Task SwapAsync_GroupToPlayer_FlattensAllTabsIntoTheGrid()
    {
        var group = CreateGroup(
            1,
            0,
            CreateTab("Tab 1", CreatePlayer("PA", 0, 0), CreatePlayer("PB", 1, 0)),
            CreateTab("Tab 2", CreatePlayer("PC", 0, 0)));

        var context = new ServiceContext(CreateGrid(CreatePlayer("P0", 0, 0), group));

        await context.Service.SwapAsync(group);

        Assert.Empty(context.Grid.Cells.OfType<Group>());
        Assert.Equal(["P0", "PA", "PB", "PC"], Names(context.Grid));
        Assert.All(context.Grid.Cells, c => Assert.Same(context.Grid, c.Grid));

        AssertNoOverlaps(context.Grid);
    }

    [Fact]
    public async Task SwapAsync_GroupToPlayer_Rejected_KeepsGroup()
    {
        var group = CreateGroup(0, 0, CreateTab("Tab 1", CreatePlayer("PA", 0, 0)));
        var context = new ServiceContext(CreateGrid(group), ButtonResult.No);

        await context.Service.SwapAsync(group);

        Assert.Same(group, context.Grid.Cells.Single());
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task SwapAsync_Null_DoesNothing()
    {
        var context = new ServiceContext(CreateGrid(CreatePlayer("A", 0, 0)));

        await context.Service.SwapAsync<Cell>(null);

        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task SwapAsync_PlayerToGroup_Rejected_KeepsPlayer()
    {
        var a = CreatePlayer("A", 0, 0);
        var context = new ServiceContext(CreateGrid(a), ButtonResult.No);

        await context.Service.SwapAsync(a);

        Assert.Same(a, context.Grid.Cells.Single());
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task SwapAsync_PlayerToGroup_WrapsPlayerInTab()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);
        var context = new ServiceContext(CreateGrid(a, b));

        await context.Service.SwapAsync(b);

        var group = Assert.IsType<Group>(context.Grid.Cells.Last());
        var tab = group.Grids.Single();

        Assert.Equal((1, 0), Position(group));
        Assert.Same(b, tab.Cells.Single());
        Assert.Same(tab, b.Grid);
        Assert.Same(group, tab.Group);
        Assert.Same(context.Grid, group.Grid);
        Assert.Equal(1, context.SaveCount);
    }

    #endregion Public Methods
}
