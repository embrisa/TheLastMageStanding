using System;
using System.Buffers;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs;

internal delegate void EcsAction<T1>(Entity entity, ref T1 component1) where T1 : struct;

internal delegate void EcsAction<T1, T2>(Entity entity, ref T1 component1, ref T2 component2)
    where T1 : struct
    where T2 : struct;

internal delegate void EcsAction<T1, T2, T3>(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3)
    where T1 : struct
    where T2 : struct
    where T3 : struct;

internal interface IComponentPool
{
    bool Remove(Entity entity);
    void RemoveAllForEntity(int entityId);
    int Count { get; }
}

/// <summary>
/// Dense component storage backed by entity-id-to-dense-index lookup.
/// Iteration snapshots entity ids through ArrayPool so systems can add/remove components during ForEach
/// without per-frame list allocation or invalidating the current traversal.
/// </summary>
internal sealed class ComponentPool<T> : IComponentPool where T : struct
{
    private const int MissingIndex = -1;
    private int[] _entityIds = [];
    private T[] _components = [];
    private int[] _entityToDenseIndex = [];
    private int _count;

    public int Count => _count;

    public void Set(Entity entity, T component)
    {
        if (TryGetDenseIndex(entity.Id, out var denseIndex))
        {
            _components[denseIndex] = component;
            return;
        }

        EnsureDenseCapacity(_count + 1);
        EnsureSparseCapacity(entity.Id + 1);

        var index = _count++;
        _entityIds[index] = entity.Id;
        _components[index] = component;
        _entityToDenseIndex[entity.Id] = index;
    }

    public bool TryGet(Entity entity, out T component)
    {
        if (TryGetDenseIndex(entity.Id, out var denseIndex))
        {
            component = _components[denseIndex];
            return true;
        }

        component = default;
        return false;
    }

    public bool Remove(Entity entity) => Remove(entity.Id);

    public void RemoveAllForEntity(int entityId) => Remove(entityId);

    public void CopyEntityIds(Span<int> destination)
    {
        _entityIds.AsSpan(0, _count).CopyTo(destination);
    }

    public bool TryGetDenseIndex(int entityId, out int denseIndex)
    {
        if ((uint)entityId < (uint)_entityToDenseIndex.Length)
        {
            denseIndex = _entityToDenseIndex[entityId];
            return denseIndex != MissingIndex;
        }

        denseIndex = MissingIndex;
        return false;
    }

    private bool Remove(int entityId)
    {
        if (!TryGetDenseIndex(entityId, out var denseIndex))
        {
            return false;
        }

        var lastIndex = _count - 1;
        if (denseIndex != lastIndex)
        {
            var movedEntityId = _entityIds[lastIndex];
            _entityIds[denseIndex] = movedEntityId;
            _components[denseIndex] = _components[lastIndex];
            _entityToDenseIndex[movedEntityId] = denseIndex;
        }

        _count--;
        _entityIds[_count] = default;
        _components[_count] = default;
        _entityToDenseIndex[entityId] = MissingIndex;
        return true;
    }

    private void EnsureDenseCapacity(int minimumSize)
    {
        if (_entityIds.Length >= minimumSize)
        {
            return;
        }

        var newSize = Math.Max(minimumSize, _entityIds.Length == 0 ? 4 : _entityIds.Length * 2);
        Array.Resize(ref _entityIds, newSize);
        Array.Resize(ref _components, newSize);
    }

    private void EnsureSparseCapacity(int minimumSize)
    {
        if (_entityToDenseIndex.Length >= minimumSize)
        {
            return;
        }

        var oldLength = _entityToDenseIndex.Length;
        var newSize = Math.Max(minimumSize, oldLength == 0 ? 4 : oldLength * 2);
        Array.Resize(ref _entityToDenseIndex, newSize);
        Array.Fill(_entityToDenseIndex, MissingIndex, oldLength, newSize - oldLength);
    }
}

internal sealed class EcsWorld
{
    public IEventBus EventBus { get; set; } = null!;

    private int _nextEntityId;
    private bool[] _alive = [];
    private readonly Dictionary<Type, IComponentPool> _componentPools = new();

    public Entity CreateEntity()
    {
        var entity = new Entity(_nextEntityId++);
        EnsureAliveCapacity(entity.Id + 1);
        _alive[entity.Id] = true;
        return entity;
    }

    public bool IsAlive(Entity entity) => entity.Id >= 0 &&
                                          entity.Id < _alive.Length &&
                                          _alive[entity.Id];

    public void DestroyEntity(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return;
        }

        _alive[entity.Id] = false;

        foreach (var pool in _componentPools.Values)
        {
            pool.RemoveAllForEntity(entity.Id);
        }
    }

    public ComponentPool<T> GetPool<T>() where T : struct
    {
        var type = typeof(T);
        if (!_componentPools.TryGetValue(type, out var pool))
        {
            pool = new ComponentPool<T>();
            _componentPools[type] = pool;
        }

        return (ComponentPool<T>)pool;
    }

    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        if (!IsAlive(entity))
        {
            return;
        }

        GetPool<T>().Set(entity, component);
    }

    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct =>
        GetPool<T>().TryGet(entity, out component);

    public bool RemoveComponent<T>(Entity entity) where T : struct => GetPool<T>().Remove(entity);

    public void ForEach<T1>(EcsAction<T1> action) where T1 : struct
    {
        var pool1 = GetPool<T1>();
        var entityIds = ArrayPool<int>.Shared.Rent(pool1.Count);
        try
        {
            var span = entityIds.AsSpan(0, pool1.Count);
            pool1.CopyEntityIds(span);
            foreach (var entityId in span)
            {
                var entity = new Entity(entityId);
                if (!pool1.TryGet(entity, out var comp1))
                {
                    continue;
                }

                var c1 = comp1;
                action(entity, ref c1);
                if (!IsAlive(entity))
                {
                    continue;
                }

                if (pool1.TryGet(entity, out _))
                {
                    pool1.Set(entity, c1);
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(entityIds);
        }
    }

    public void ForEach<T1, T2>(EcsAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var sourceCount = Math.Min(pool1.Count, pool2.Count);
        var entityIds = ArrayPool<int>.Shared.Rent(sourceCount);
        try
        {
            var span = entityIds.AsSpan(0, sourceCount);
            if (pool1.Count <= pool2.Count)
            {
                pool1.CopyEntityIds(span);
            }
            else
            {
                pool2.CopyEntityIds(span);
            }

            foreach (var entityId in span)
            {
                var entity = new Entity(entityId);
                if (!pool1.TryGet(entity, out var comp1) || !pool2.TryGet(entity, out var comp2))
                {
                    continue;
                }

                var c1 = comp1;
                var c2 = comp2;
                action(entity, ref c1, ref c2);
                if (!IsAlive(entity))
                {
                    continue;
                }

                if (pool1.TryGet(entity, out _))
                {
                    pool1.Set(entity, c1);
                }

                if (pool2.TryGet(entity, out _))
                {
                    pool2.Set(entity, c2);
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(entityIds);
        }
    }

    public void ForEach<T1, T2, T3>(EcsAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var sourceCount = Math.Min(pool1.Count, Math.Min(pool2.Count, pool3.Count));
        var entityIds = ArrayPool<int>.Shared.Rent(sourceCount);
        try
        {
            var span = entityIds.AsSpan(0, sourceCount);
            if (pool1.Count <= pool2.Count && pool1.Count <= pool3.Count)
            {
                pool1.CopyEntityIds(span);
            }
            else if (pool2.Count <= pool3.Count)
            {
                pool2.CopyEntityIds(span);
            }
            else
            {
                pool3.CopyEntityIds(span);
            }

            foreach (var entityId in span)
            {
                var entity = new Entity(entityId);
                if (!pool1.TryGet(entity, out var comp1) ||
                    !pool2.TryGet(entity, out var comp2) ||
                    !pool3.TryGet(entity, out var comp3))
                {
                    continue;
                }

                var c1 = comp1;
                var c2 = comp2;
                var c3 = comp3;
                action(entity, ref c1, ref c2, ref c3);
                if (!IsAlive(entity))
                {
                    continue;
                }

                if (pool1.TryGet(entity, out _))
                {
                    pool1.Set(entity, c1);
                }

                if (pool2.TryGet(entity, out _))
                {
                    pool2.Set(entity, c2);
                }

                if (pool3.TryGet(entity, out _))
                {
                    pool3.Set(entity, c3);
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(entityIds);
        }
    }

    private void EnsureAliveCapacity(int minimumSize)
    {
        if (_alive.Length >= minimumSize)
        {
            return;
        }

        var newSize = Math.Max(minimumSize, _alive.Length == 0 ? 4 : _alive.Length * 2);
        Array.Resize(ref _alive, newSize);
    }
}
