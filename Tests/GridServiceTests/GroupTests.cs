using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class GroupTests
{
    #region Public Methods

    [Fact]
    public async Task GroupAsync_Null_DoesNothing()
    {
        var context = new ServiceContext(CreateGrid(CreatePlayer("A", 0, 0)));

        await context.Service.GroupAsync(null);

        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task GroupAsync_Rejected_KeepsPlayer()
    {
        var a = CreatePlayer("A", 0, 0);
        var context = new ServiceContext(CreateGrid(a), ButtonResult.No);

        await context.Service.GroupAsync(a);

        Assert.Same(a, context.Grid.Cells.Single());
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task GroupAsync_WrapsPlayerInTab()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);
        var context = new ServiceContext(CreateGrid(a, b));

        await context.Service.GroupAsync(b);

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
