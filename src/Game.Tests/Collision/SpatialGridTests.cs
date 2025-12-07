using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Collision;

public class SpatialGridTests
{
    [Fact]
    public void Insert_AddsEntityToCorrectCells()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        var bounds = new Rectangle(50, 50, 60, 60); // Spans from (50,50) to (110,110)

        // Act
        grid.Insert(entityId: 1, bounds);
        var nearby = grid.QueryNearby(bounds);

        // Assert
        Assert.Contains(1, nearby);
    }

    [Fact]
    public void QueryPotentialPairs_ReturnsEntitiesInSameCell()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        var bounds1 = new Rectangle(10, 10, 20, 20);
        var bounds2 = new Rectangle(30, 30, 20, 20);

        // Act
        grid.Insert(1, bounds1);
        grid.Insert(2, bounds2);
        var pairs = grid.QueryPotentialPairs();

        // Assert
        Assert.Contains((1, 2), pairs);
    }

    [Fact]
    public void QueryPotentialPairs_DoesNotReturnDistantEntities()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        var bounds1 = new Rectangle(10, 10, 20, 20);
        var bounds2 = new Rectangle(500, 500, 20, 20); // Far away

        // Act
        grid.Insert(1, bounds1);
        grid.Insert(2, bounds2);
        var pairs = grid.QueryPotentialPairs();

        // Assert
        Assert.DoesNotContain((1, 2), pairs);
    }

    [Fact]
    public void Remove_RemovesEntityFromGrid()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        var bounds = new Rectangle(10, 10, 20, 20);
        grid.Insert(1, bounds);

        // Act
        grid.Remove(1);
        var nearby = grid.QueryNearby(bounds);

        // Assert
        Assert.DoesNotContain(1, nearby);
    }

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        grid.Insert(1, new Rectangle(10, 10, 20, 20));
        grid.Insert(2, new Rectangle(50, 50, 20, 20));

        // Act
        grid.Clear();
        var pairs = grid.QueryPotentialPairs();

        // Assert
        Assert.Empty(pairs);
    }

    [Fact]
    public void QueryNearby_ReturnsEntitiesInAdjacentCells()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        grid.Insert(1, new Rectangle(10, 10, 20, 20));    // Cell (0,0)
        grid.Insert(2, new Rectangle(110, 10, 20, 20));   // Cell (1,0)

        // Act
        var nearby = grid.QueryNearby(new Rectangle(10, 10, 20, 20));

        // Assert
        Assert.Contains(1, nearby);
        // Entity 2 is in adjacent cell but QueryNearby only returns same cells
        Assert.DoesNotContain(2, nearby);
    }

    [Fact]
    public void Insert_LargeEntity_SpansMultipleCells()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        var largeBounds = new Rectangle(50, 50, 150, 150); // Spans multiple cells

        // Act
        grid.Insert(1, largeBounds);
        var nearby1 = grid.QueryNearby(new Rectangle(60, 60, 10, 10));
        var nearby2 = grid.QueryNearby(new Rectangle(180, 180, 10, 10));

        // Assert - Entity should be found in both regions
        Assert.Contains(1, nearby1);
        Assert.Contains(1, nearby2);
    }

    [Fact]
    public void QueryPotentialPairs_OrdersEntityIdsConsistently()
    {
        // Arrange
        var grid = new SpatialGrid(cellSize: 100f);
        grid.Insert(5, new Rectangle(10, 10, 20, 20));
        grid.Insert(3, new Rectangle(30, 30, 20, 20));

        // Act
        var pairs = grid.QueryPotentialPairs();

        // Assert - Lower ID should be first
        Assert.Contains((3, 5), pairs);
        Assert.DoesNotContain((5, 3), pairs);
    }
}
