using System;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class MoveTests
{
    #region Public Methods

    [Fact]
    public void MoveCell_Bottom_IgnoresDiagonalNeighbor()
    {
        var z = CreatePlayer("Z", 0, 0);
        var x = CreatePlayer("X", 1, 0);
        var y = CreatePlayer("Y", 0, 1);

        var context = new ServiceContext(CreateGrid(z, x, y));

        context.Service.MoveCell(x, OrientationType.Bottom);

        Assert.Equal((0, 0), Position(z));
        Assert.Equal((1, 1), Position(x));
        Assert.Equal((0, 1), Position(y));
    }

    [Fact]
    public void MoveCell_Bottom_SwapsWithNeighborBelow()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 0, 1);

        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.MoveCell(a, OrientationType.Bottom);

        Assert.Equal((0, 1), Position(a));
        Assert.Equal((0, 0), Position(b));
    }

    [Fact]
    public void MoveCell_Left_IgnoresDiagonalNeighbor()
    {
        var y = CreatePlayer("Y", 0, 0);
        var x = CreatePlayer("X", 1, 1);

        var context = new ServiceContext(CreateGrid(y, x));

        context.Service.MoveCell(x, OrientationType.Left);

        Assert.Equal((0, 0), Position(y));
        Assert.Equal((0, 1), Position(x));
    }

    [Fact]
    public void MoveCell_Left_SwapsWithLeftNeighbor()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);

        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.MoveCell(b, OrientationType.Left);

        Assert.Equal((1, 0), Position(a));
        Assert.Equal((0, 0), Position(b));
    }

    [Fact]
    public void MoveCell_Null_DoesNothing()
    {
        var context = new ServiceContext(CreateGrid(CreatePlayer("A", 0, 0)));

        context.Service.MoveCell(null, OrientationType.Right);

        Assert.Equal(0, context.SaveCount);
        Assert.Equal(0, context.UpdateCount);
    }

    [Fact]
    public void MoveCell_Right_IgnoresDiagonalNeighbor()
    {
        var y = CreatePlayer("Y", 1, 0);
        var x = CreatePlayer("X", 0, 1);

        var context = new ServiceContext(CreateGrid(y, x));

        context.Service.MoveCell(x, OrientationType.Right);

        // Both end up in the same column, the empty column is removed.
        Assert.Equal((0, 0), Position(y));
        Assert.Equal((0, 1), Position(x));
    }

    [Fact]
    public void MoveCell_Right_IntoEmptySpace_DoesNotMoveOthers()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 2, 0);
        var z = CreatePlayer("Z", 0, 1);

        var context = new ServiceContext(CreateGrid(a, b, z));

        context.Service.MoveCell(a, OrientationType.Right);

        Assert.Equal((1, 0), Position(a));
        Assert.Equal((2, 0), Position(b));
        Assert.Equal((0, 1), Position(z));
    }

    [Fact]
    public void MoveCell_Right_SavesAndPublishesOnce()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);

        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.MoveCell(a, OrientationType.Right);

        Assert.Equal(1, context.SaveCount);
        Assert.Equal(1, context.UpdateCount);
    }

    [Fact]
    public void MoveCell_Right_SwapsWithRightNeighbor()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 1, 0);

        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.MoveCell(a, OrientationType.Right);

        Assert.Equal((1, 0), Position(a));
        Assert.Equal((0, 0), Position(b));
    }

    [Fact]
    public void MoveCell_Top_IgnoresDiagonalNeighbor()
    {
        var y = CreatePlayer("Y", 0, 0);
        var z = CreatePlayer("Z", 0, 1);
        var x = CreatePlayer("X", 1, 1);

        var context = new ServiceContext(CreateGrid(y, z, x));

        context.Service.MoveCell(x, OrientationType.Top);

        Assert.Equal((0, 0), Position(y));
        Assert.Equal((0, 1), Position(z));
        Assert.Equal((1, 0), Position(x));
    }

    [Fact]
    public void MoveCell_Top_SwapsWithNeighborAbove()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 0, 1);

        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.MoveCell(b, OrientationType.Top);

        Assert.Equal((0, 1), Position(a));
        Assert.Equal((0, 0), Position(b));
    }

    [Fact]
    public void MoveGrid_FirstTabLeft_DoesNothing()
    {
        var context = CreateTabContext(out var group);

        context.Service.MoveGrid(group.Grids[0], OrientationType.Left);

        Assert.Equal(["Tab 1", "Tab 2", "Tab 3"], TabNames(group));
    }

    [Fact]
    public void MoveGrid_InvalidOrientation_ThrowsAndKeepsState()
    {
        var context = CreateTabContext(out var group);
        var before = Describe(context.Grid);

        Assert.Throws<ArgumentException>(
            () => context.Service.MoveGrid(group.Grids[0], OrientationType.Top));

        Assert.Equal(before, Describe(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void MoveGrid_LastTabRight_DoesNothing()
    {
        var context = CreateTabContext(out var group);

        context.Service.MoveGrid(group.Grids[2], OrientationType.Right);

        Assert.Equal(["Tab 1", "Tab 2", "Tab 3"], TabNames(group));
    }

    [Fact]
    public void MoveGrid_Left_SwapsWithLeftTab()
    {
        var context = CreateTabContext(out var group);

        context.Service.MoveGrid(group.Grids[2], OrientationType.Left);

        Assert.Equal(["Tab 1", "Tab 3", "Tab 2"], TabNames(group));
    }

    [Fact]
    public void MoveGrid_Right_SwapsWithRightTab()
    {
        var context = CreateTabContext(out var group);

        context.Service.MoveGrid(group.Grids[0], OrientationType.Right);

        Assert.Equal(["Tab 2", "Tab 1", "Tab 3"], TabNames(group));
    }

    #endregion Public Methods

    #region Private Methods

    private static ServiceContext CreateTabContext(out Group group)
    {
        group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", CreatePlayer("A", 0, 0)),
            CreateTab("Tab 2", CreatePlayer("B", 0, 0)),
            CreateTab("Tab 3", CreatePlayer("C", 0, 0)));

        return new ServiceContext(CreateGrid(group));
    }

    #endregion Private Methods
}
