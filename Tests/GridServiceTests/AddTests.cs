using System;
using System.Linq;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class AddTests
{
    #region Public Methods

    [Fact]
    public void AddGrid_InvalidOrientation_ThrowsAndKeepsState()
    {
        var context = CreateTabContext(out var group);
        var before = Describe(context.Grid);

        Assert.Throws<ArgumentException>(
            () => context.Service.AddGrid(group.Grids[0], OrientationType.Top));

        Assert.Equal(before, Describe(context.Grid));
        Assert.Equal(0, context.SaveCount);
    }

    [Fact]
    public void AddGrid_Left_InsertsDirectlyBeforeAnchor()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[1], OrientationType.Left);

        Assert.Equal(["Tab 1", "Tab 4", "Tab 2", "Tab 3"], TabNames(group));
    }

    [Fact]
    public void AddGrid_Left_OnFirstTab_InsertsAtStart()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[0], OrientationType.Left);

        Assert.Equal(["Tab 4", "Tab 1", "Tab 2", "Tab 3"], TabNames(group));
    }

    [Fact]
    public void AddGrid_NameGap_UsesFirstFreeNumber()
    {
        var group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", CreatePlayer("A", 0, 0)),
            CreateTab("Tab 3", CreatePlayer("B", 0, 0)));

        var context = new ServiceContext(CreateGrid(group));

        context.Service.AddGrid(group.Grids[1], OrientationType.Right);

        Assert.Equal(["Tab 1", "Tab 3", "Tab 2"], TabNames(group));
    }

    [Fact]
    public void AddGrid_NewTabContainsDefaultPlayer()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[2], OrientationType.Right);

        var tab = group.Grids[3];

        Assert.IsType<Player>(tab.Cells.Single());
        Assert.Same(group, tab.Group);
    }

    [Fact]
    public void AddGrid_Right_InsertsDirectlyAfterAnchor()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[0], OrientationType.Right);

        Assert.Equal(["Tab 1", "Tab 4", "Tab 2", "Tab 3"], TabNames(group));
    }

    [Fact]
    public void AddGrid_Right_OnLastTab_AppendsAtEnd()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[2], OrientationType.Right);

        Assert.Equal(["Tab 1", "Tab 2", "Tab 3", "Tab 4"], TabNames(group));
    }

    [Fact]
    public void AddGrid_SavesAndPublishesOnce()
    {
        var context = CreateTabContext(out var group);

        context.Service.AddGrid(group.Grids[0], OrientationType.Right);

        Assert.Equal(1, context.SaveCount);
        Assert.Equal(1, context.UpdateCount);
    }

    [Fact]
    public void AddPlayer_Bottom_AddsPlayerBelowAnchor()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        context.Service.AddPlayer(a, OrientationType.Bottom);

        Assert.Equal(["Player 1", "Player 2"], Names(context.Grid));
        Assert.Equal((0, 0), Position(a));
        Assert.Equal((0, 1), Position(Find(context.Grid, "Player 2")));
    }

    [Fact]
    public void AddPlayer_Left_AtLeftEdge_ShiftsAnchorRight()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        context.Service.AddPlayer(a, OrientationType.Left);

        Assert.Equal((1, 0), Position(a));
        Assert.Equal((0, 0), Position(Find(context.Grid, "Player 2")));
    }

    [Fact]
    public void AddPlayer_NameGap_UsesFirstFreeNumber()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var b = CreatePlayer("Player 3", 1, 0);
        var context = new ServiceContext(CreateGrid(a, b));

        context.Service.AddPlayer(b, OrientationType.Right);

        Assert.Equal((2, 0), Position(Find(context.Grid, "Player 2")));
    }

    [Fact]
    public void AddPlayer_Right_AddsPlayerNextToAnchor()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        context.Service.AddPlayer(a, OrientationType.Right);

        Assert.Equal(["Player 1", "Player 2"], Names(context.Grid));
        Assert.Equal((0, 0), Position(a));
        Assert.Equal((1, 0), Position(Find(context.Grid, "Player 2")));
    }

    [Fact]
    public void AddPlayer_Right_SavesAndPublishesOnce()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        context.Service.AddPlayer(a, OrientationType.Right);

        Assert.Equal(1, context.SaveCount);
        Assert.Equal(1, context.UpdateCount);
    }

    [Fact]
    public void AddPlayer_ToPlayerInsideTab_AddsToTheTab()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var tab = CreateTab("Tab 1", a);
        var context = new ServiceContext(CreateGrid(CreateGroup(0, 0, tab)));

        context.Service.AddPlayer(a, OrientationType.Right);

        Assert.Equal(2, tab.Cells.Count);
        Assert.IsType<Group>(context.Grid.Cells.Single());
    }

    [Fact]
    public void AddPlayer_Top_AtTopEdge_ShiftsAnchorDown()
    {
        var a = CreatePlayer("Player 1", 0, 0);
        var context = new ServiceContext(CreateGrid(a));

        context.Service.AddPlayer(a, OrientationType.Top);

        Assert.Equal((0, 1), Position(a));
        Assert.Equal((0, 0), Position(Find(context.Grid, "Player 2")));
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
