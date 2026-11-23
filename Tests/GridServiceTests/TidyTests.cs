using System;
using System.Linq;
using SportDJ.Commons.Models.Structure;
using Xunit;
using static SportDJ.Tests.GridServiceTests.TestHelper;

namespace SportDJ.Tests.GridServiceTests;

public class TidyTests
{
    #region Public Methods

    [Fact]
    public void Load_GroupIsWiderThanNeighborDistance_PushesNeighborBehindGroup()
    {
        var group = CreateGroup(
            0,
            0,
            CreateTab("Tab 1", CreatePlayer("A", 0, 0), CreatePlayer("B", 1, 0), CreatePlayer("C", 2, 0)),
            CreateTab("Tab 2", CreatePlayer("D", 0, 0)));

        var neighbor = CreatePlayer("N", 1, 0);

        var context = new ServiceContext(CreateGrid(group, neighbor));

        Assert.Equal(3, group.Columns);
        Assert.Equal(1, group.Rows);
        Assert.Equal((3, 0), Position(neighbor));
        Assert.Equal(4, context.Grid.Columns);
        Assert.Equal(1, context.Grid.Rows);
    }

    [Fact]
    public void Load_OffsetCells_AreMovedToOrigin()
    {
        var a = CreatePlayer("A", 5, 3);
        var b = CreatePlayer("B", 6, 3);

        var context = new ServiceContext(CreateGrid(a, b));

        Assert.Equal((0, 0), Position(a));
        Assert.Equal((1, 0), Position(b));
        Assert.Equal(2, context.Grid.Columns);
        Assert.Equal(1, context.Grid.Rows);
    }

    [Fact]
    public void Load_OverlappingCells_AreSeparated()
    {
        var a = CreatePlayer("A", 0, 0);
        var b = CreatePlayer("B", 0, 0);

        var context = new ServiceContext(CreateGrid(a, b));

        Assert.NotEqual(Position(a), Position(b));

        AssertNoOverlaps(context.Grid);
    }

    [Fact]
    public void Load_RandomLayoutsWithGroups_NeverOverlap()
    {
        var random = new Random(4321);

        for (var run = 0; run < 300; run++)
        {
            var cells = Enumerable.Range(0, random.Next(1, 8))
                .Select(i => i % 3 == 0
                    ? CreateRandomGroup(random, i)
                    : CreatePlayer($"P{i}", random.Next(0, 4), random.Next(0, 4)))
                .ToArray();

            var context = new ServiceContext(CreateGrid(cells));

            AssertNoOverlaps(context.Grid);
        }
    }

    [Fact]
    public void Load_RandomLayouts_NeverOverlapAndAreStable()
    {
        var random = new Random(1234);

        for (var run = 0; run < 300; run++)
        {
            var cells = Enumerable.Range(0, random.Next(1, 13))
                .Select(i => (Cell)CreatePlayer($"P{i}", random.Next(0, 5), random.Next(0, 5)))
                .ToArray();

            var context = new ServiceContext(CreateGrid(cells));

            AssertNoOverlaps(context.Grid);

            var tidied = Describe(context.Grid);

            context.Reload();

            Assert.Equal(tidied, Describe(context.Grid));
        }
    }

    [Fact]
    public void Load_SetsBackReferences()
    {
        var player = CreatePlayer("A", 0, 0);
        var tab = CreateTab("Tab 1", player);
        var group = CreateGroup(0, 0, tab);

        var context = new ServiceContext(CreateGrid(group));

        Assert.Same(context.Grid, group.Grid);
        Assert.Same(group, tab.Group);
        Assert.Same(tab, player.Grid);
    }

    [Fact]
    public void Load_UnsortedCells_AreSortedByRowThenColumn()
    {
        var context = new ServiceContext(CreateGrid(
            CreatePlayer("D", 1, 1),
            CreatePlayer("B", 1, 0),
            CreatePlayer("C", 0, 1),
            CreatePlayer("A", 0, 0)));

        Assert.Equal(["A", "B", "C", "D"], Names(context.Grid));
    }

    #endregion Public Methods

    #region Private Methods

    private static Cell CreateRandomGroup(Random random, int index)
    {
        var tabs = Enumerable.Range(0, random.Next(1, 4))
            .Select(t => CreateTab(
                $"Tab {t + 1}",
                [.. Enumerable.Range(0, random.Next(1, 4))
                    .Select(p => (Cell)CreatePlayer($"G{index}T{t}P{p}", p, random.Next(0, 2)))]))
            .ToArray();

        return CreateGroup(random.Next(0, 4), random.Next(0, 4), tabs);
    }

    #endregion Private Methods
}
