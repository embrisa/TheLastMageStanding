using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

/// <summary>
/// Spatial grid for efficient broadphase collision detection.
/// Divides space into fixed-size cells and tracks which entities occupy each cell.
/// </summary>
internal sealed class SpatialGrid
{
    private readonly float _cellSize;
    private readonly Dictionary<(int x, int y), List<int>> _grid = new();
    private readonly Dictionary<int, HashSet<(int x, int y)>> _entityCells = new();

    public SpatialGrid(float cellSize = 128f)
    {
        if (cellSize <= 0f)
            throw new ArgumentException("Cell size must be positive", nameof(cellSize));
        
        _cellSize = cellSize;
    }

    /// <summary>
    /// Clears all entities from the grid.
    /// </summary>
    public void Clear()
    {
        _grid.Clear();
        _entityCells.Clear();
    }

    /// <summary>
    /// Inserts an entity into the grid based on its bounding rectangle.
    /// </summary>
    public void Insert(int entityId, Rectangle bounds)
    {
        // Calculate cell range covered by the bounds
        var minCell = GetCell(new Vector2(bounds.Left, bounds.Top));
        var maxCell = GetCell(new Vector2(bounds.Right, bounds.Bottom));

        // Track which cells this entity occupies
        if (!_entityCells.TryGetValue(entityId, out var cells))
        {
            cells = new HashSet<(int x, int y)>();
            _entityCells[entityId] = cells;
        }
        else
        {
            cells.Clear();
        }

        // Insert entity into all cells it overlaps
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cellKey = (x, y);
                
                if (!_grid.TryGetValue(cellKey, out var entityList))
                {
                    entityList = new List<int>();
                    _grid[cellKey] = entityList;
                }
                
                entityList.Add(entityId);
                cells.Add(cellKey);
            }
        }
    }

    /// <summary>
    /// Removes an entity from all cells it occupies.
    /// </summary>
    public void Remove(int entityId)
    {
        if (!_entityCells.TryGetValue(entityId, out var cells))
            return;

        foreach (var cellKey in cells)
        {
            if (_grid.TryGetValue(cellKey, out var entityList))
            {
                entityList.Remove(entityId);
                
                // Clean up empty cells to avoid memory bloat
                if (entityList.Count == 0)
                {
                    _grid.Remove(cellKey);
                }
            }
        }

        _entityCells.Remove(entityId);
    }

    /// <summary>
    /// Queries all potential collision pairs within the grid.
    /// Returns unique pairs (a, b) where a &lt; b to avoid duplicates.
    /// </summary>
    public HashSet<(int entityA, int entityB)> QueryPotentialPairs()
    {
        var pairs = new HashSet<(int entityA, int entityB)>();

        foreach (var entityList in _grid.Values)
        {
            // Check all entity pairs within each cell
            for (int i = 0; i < entityList.Count; i++)
            {
                for (int j = i + 1; j < entityList.Count; j++)
                {
                    var a = entityList[i];
                    var b = entityList[j];
                    
                    // Ensure consistent ordering (lower ID first)
                    if (a > b)
                        (a, b) = (b, a);
                    
                    pairs.Add((a, b));
                }
            }
        }

        return pairs;
    }

    /// <summary>
    /// Queries entities near the given bounds (within the same or adjacent cells).
    /// </summary>
    public HashSet<int> QueryNearby(Rectangle bounds)
    {
        var results = new HashSet<int>();
        
        var minCell = GetCell(new Vector2(bounds.Left, bounds.Top));
        var maxCell = GetCell(new Vector2(bounds.Right, bounds.Bottom));

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cellKey = (x, y);
                
                if (_grid.TryGetValue(cellKey, out var entityList))
                {
                    foreach (var entityId in entityList)
                    {
                        results.Add(entityId);
                    }
                }
            }
        }

        return results;
    }

    private (int x, int y) GetCell(Vector2 position)
    {
        return (
            (int)MathF.Floor(position.X / _cellSize),
            (int)MathF.Floor(position.Y / _cellSize)
        );
    }
}
