using System.Linq;
using System.Threading.Tasks;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class LimitTests
{
    #region Private Fields

    private const int Max = Services.GridService.Service.MaxCount;

    #endregion Private Fields

    #region Public Methods

    [Fact]
    public void AddGrid_AtMaxTabs_IsIgnored()
    {
        var group = CreateGroupWithTabs(Max);
        var context = new ServiceContext(CreateGrid(group));
        var before = Describe(context.Grid);

        context.Service.AddGrid(group.Grids.Last(), OrientationType.Right);

        Assert.Equal(Max, group.Grids.Count);
        Assert.Equal(before, Describe(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void AddGrid_BelowMaxTabs_IsAllowed()
    {
        var group = CreateGroupWithTabs(Max - 1);
        var context = new ServiceContext(CreateGrid(group));

        context.Service.AddGrid(group.Grids.Last(), OrientationType.Right);

        Assert.Equal(Max, group.Grids.Count);
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_AtMaxColumns_LeftAndRightAreIgnored()
    {
        var grid = CreateRow(Max);
        var context = new ServiceContext(grid);
        var before = Describe(context.Grid);

        context.Service.AddPlayer(context.Grid.Cells.First(), OrientationType.Left);
        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Right);

        Assert.Equal(Max, context.Grid.Columns);
        Assert.Equal(before, Describe(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_AtMaxRows_TopAndBottomAreIgnored()
    {
        var grid = CreateColumn(Max);
        var context = new ServiceContext(grid);
        var before = Describe(context.Grid);

        context.Service.AddPlayer(context.Grid.Cells.First(), OrientationType.Top);
        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Bottom);

        Assert.Equal(Max, context.Grid.Rows);
        Assert.Equal(before, Describe(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_BelowMaxColumns_IsAllowed()
    {
        var context = new ServiceContext(CreateRow(Max - 1));

        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Right);

        Assert.Equal(Max, context.Grid.Columns);
        Assert.Equal(Max, context.Grid.Cells.Count);
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_BelowMaxRows_IsAllowed()
    {
        var context = new ServiceContext(CreateColumn(Max - 1));

        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Bottom);

        Assert.Equal(Max, context.Grid.Rows);
        Assert.Equal(Max, context.Grid.Cells.Count);
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_InShorterRowOfWideGrid_IsAllowed()
    {
        // The limit applies to the size of the grid, not to the length of a single row.
        var cells = CreateRow(Max).Cells
            .Append(CreatePlayer("Short", 0, 1)).ToArray();

        var context = new ServiceContext(CreateGrid(cells));

        context.Service.AddPlayer(Find(context.Grid, "Short"), OrientationType.Right);

        Assert.Equal(Max + 2, context.Grid.Cells.Count);
        Assert.Equal(Max, context.Grid.Columns);
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_InTabAtMaxColumns_IsIgnored()
    {
        var tab = CreateTab("Tab 1", [.. CreateRow(Max).Cells]);
        var context = new ServiceContext(CreateGrid(CreateGroup(0, 0, tab)));

        context.Service.AddPlayer(tab.Cells.Last(), OrientationType.Right);

        Assert.Equal(Max, tab.Cells.Count);
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void AddPlayer_InTabWhichWouldWidenParentBeyondMax_IsIgnored()
    {
        var tab = CreateTab("Tab 1", [.. CreateRow(Max - 1).Cells]);
        var group = CreateGroup(1, 0, tab);

        var context = new ServiceContext(CreateGrid(CreatePlayer("Left", 0, 0), group));

        Assert.Equal(Max, context.Grid.Columns);

        context.Service.AddPlayer(tab.Cells.Last(), OrientationType.Right);

        Assert.Equal(Max - 1, tab.Cells.Count);
        Assert.Equal(Max, context.Grid.Columns);
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void MaxCount_Is256()
    {
        Assert.Equal(256, Max);
    }

    [Fact]
    public void MoveCell_OverTheLeftEdge_CannotGrowBeyondMax()
    {
        var context = new ServiceContext(CreateRow(Max));
        var before = Describe(context.Grid);

        for (var i = 0; i < 3; i++)
        {
            context.Service.MoveCell(context.Grid.Cells.First(), OrientationType.Left);
        }

        Assert.True(context.Grid.Columns <= Max);
        Assert.Equal(before, Describe(context.Grid));
    }

    [Fact]
    public void OversizedGrid_CannotGrowFurther()
    {
        var context = new ServiceContext(CreateRow(Max + 44));

        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Right);

        Assert.Equal(Max + 44, context.Grid.Cells.Count);
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public async Task OversizedGrid_CanStillBeReduced()
    {
        var context = new ServiceContext(CreateRow(Max + 44));

        await context.Service.RemoveCellAsync(context.Grid.Cells.Last());

        Assert.Equal(Max + 43, context.Grid.Cells.Count);
        Assert.Equal(1, context.SaveCount);
    }

    [Fact]
    public void RejectedOperation_PublishesUpdateButDoesNotSave()
    {
        var context = new ServiceContext(CreateRow(Max));

        context.Service.AddPlayer(context.Grid.Cells.Last(), OrientationType.Right);

        Assert.Equal(0, context.SaveCount);
        Assert.Equal(1, context.UpdateCount);
    }

    [Fact]
    public async Task RemoveGridAsync_BeyondMaxColumns_RemovesTab()
    {
        var group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", [.. CreateRow(200, "A").Cells]),
            CreateTab("Tab 2", [.. CreateRow(200, "B").Cells]));

        var context = new ServiceContext(CreateGrid(group));

        // Removing a tab never grows the grid, so it is allowed even if the remaining one is oversized
        await context.Service.RemoveGridAsync(group.Grids[0]);

        Assert.Same(group, context.Grid.Cells.Single());
        Assert.Equal(["Tab 2"], TabNames(group));
        Assert.Equal(1, context.SaveCount);
    }

    #endregion Public Methods

    #region Private Methods

    private static Group CreateGroupWithTabs(int count)
    {
        return CreateGroup(
            0,
            0,
            [.. Enumerable.Range(1, count)
                .Select(i => CreateTab($"Tab {i}", CreatePlayer("A", 0, 0)))]);
    }

    #endregion Private Methods
}
