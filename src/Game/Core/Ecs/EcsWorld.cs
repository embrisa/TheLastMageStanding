using System;
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

    public T GetByDenseIndex(int denseIndex) => _components[denseIndex];

    public void WriteBackIfPresent(int entityId, int originalDenseIndex, T component)
    {
        if ((uint)originalDenseIndex < (uint)_count && _entityIds[originalDenseIndex] == entityId)
        {
            _components[originalDenseIndex] = component;
            return;
        }

        if (TryGetDenseIndex(entityId, out var currentDenseIndex))
        {
            _components[currentDenseIndex] = component;
        }
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

internal sealed class EcsQuery<T1>(EcsWorld world)
    where T1 : struct
{
    private readonly EcsWorld _world = world;
    private readonly ComponentPool<T1> _pool1 = world.GetPool<T1>();
    private int[] _entityIds = [];

    public void ForEach(EcsAction<T1> action)
    {
        var count = _pool1.Count;
        if (count == 0)
        {
            return;
        }

        var entityIds = EnsureEntityBuffer(count).AsSpan(0, count);
        _pool1.CopyEntityIds(entityIds);

        foreach (var entityId in entityIds)
        {
            if (!_pool1.TryGetDenseIndex(entityId, out var denseIndex))
            {
                continue;
            }

            var component1 = _pool1.GetByDenseIndex(denseIndex);
            var entity = new Entity(entityId);
            action(entity, ref component1);
            if (!_world.IsAlive(entity))
            {
                continue;
            }

            _pool1.WriteBackIfPresent(entityId, denseIndex, component1);
        }
    }

    private int[] EnsureEntityBuffer(int count)
    {
        if (_entityIds.Length < count)
        {
            Array.Resize(ref _entityIds, count);
        }

        return _entityIds;
    }
}

internal sealed class EcsQuery<T1, T2>(EcsWorld world)
    where T1 : struct
    where T2 : struct
{
    private readonly EcsWorld _world = world;
    private readonly ComponentPool<T1> _pool1 = world.GetPool<T1>();
    private readonly ComponentPool<T2> _pool2 = world.GetPool<T2>();
    private int[] _entityIds = [];

    public void ForEach(EcsAction<T1, T2> action)
    {
        var sourceCount = Math.Min(_pool1.Count, _pool2.Count);
        if (sourceCount == 0)
        {
            return;
        }

        var entityIds = EnsureEntityBuffer(sourceCount).AsSpan(0, sourceCount);
        if (_pool1.Count <= _pool2.Count)
        {
            _pool1.CopyEntityIds(entityIds);
        }
        else
        {
            _pool2.CopyEntityIds(entityIds);
        }

        foreach (var entityId in entityIds)
        {
            if (!_pool1.TryGetDenseIndex(entityId, out var denseIndex1) ||
                !_pool2.TryGetDenseIndex(entityId, out var denseIndex2))
            {
                continue;
            }

            var component1 = _pool1.GetByDenseIndex(denseIndex1);
            var component2 = _pool2.GetByDenseIndex(denseIndex2);
            var entity = new Entity(entityId);
            action(entity, ref component1, ref component2);
            if (!_world.IsAlive(entity))
            {
                continue;
            }

            _pool1.WriteBackIfPresent(entityId, denseIndex1, component1);
            _pool2.WriteBackIfPresent(entityId, denseIndex2, component2);
        }
    }

    private int[] EnsureEntityBuffer(int count)
    {
        if (_entityIds.Length < count)
        {
            Array.Resize(ref _entityIds, count);
        }

        return _entityIds;
    }
}

internal sealed class EcsQuery<T1, T2, T3>(EcsWorld world)
    where T1 : struct
    where T2 : struct
    where T3 : struct
{
    private readonly EcsWorld _world = world;
    private readonly ComponentPool<T1> _pool1 = world.GetPool<T1>();
    private readonly ComponentPool<T2> _pool2 = world.GetPool<T2>();
    private readonly ComponentPool<T3> _pool3 = world.GetPool<T3>();
    private int[] _entityIds = [];

    public void ForEach(EcsAction<T1, T2, T3> action)
    {
        var sourceCount = Math.Min(_pool1.Count, Math.Min(_pool2.Count, _pool3.Count));
        if (sourceCount == 0)
        {
            return;
        }

        var entityIds = EnsureEntityBuffer(sourceCount).AsSpan(0, sourceCount);
        if (_pool1.Count <= _pool2.Count && _pool1.Count <= _pool3.Count)
        {
            _pool1.CopyEntityIds(entityIds);
        }
        else if (_pool2.Count <= _pool3.Count)
        {
            _pool2.CopyEntityIds(entityIds);
        }
        else
        {
            _pool3.CopyEntityIds(entityIds);
        }

        foreach (var entityId in entityIds)
        {
            if (!_pool1.TryGetDenseIndex(entityId, out var denseIndex1) ||
                !_pool2.TryGetDenseIndex(entityId, out var denseIndex2) ||
                !_pool3.TryGetDenseIndex(entityId, out var denseIndex3))
            {
                continue;
            }

            var component1 = _pool1.GetByDenseIndex(denseIndex1);
            var component2 = _pool2.GetByDenseIndex(denseIndex2);
            var component3 = _pool3.GetByDenseIndex(denseIndex3);
            var entity = new Entity(entityId);
            action(entity, ref component1, ref component2, ref component3);
            if (!_world.IsAlive(entity))
            {
                continue;
            }

            _pool1.WriteBackIfPresent(entityId, denseIndex1, component1);
            _pool2.WriteBackIfPresent(entityId, denseIndex2, component2);
            _pool3.WriteBackIfPresent(entityId, denseIndex3, component3);
        }
    }

    private int[] EnsureEntityBuffer(int count)
    {
        if (_entityIds.Length < count)
        {
            Array.Resize(ref _entityIds, count);
        }

        return _entityIds;
    }
}

internal sealed class EcsWorld
{
    public IEventBus EventBus { get; set; } = null!;

    private int _nextEntityId;
    private bool[] _alive = [];
    private readonly Dictionary<Type, IComponentPool> _componentPools = new();
    private readonly Dictionary<Type, object> _queryCache1 = new();
    private readonly Dictionary<(Type, Type), object> _queryCache2 = new();
    private readonly Dictionary<(Type, Type, Type), object> _queryCache3 = new();

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

    public EcsQuery<T1> Query<T1>() where T1 : struct
    {
        var type = typeof(T1);
        if (!_queryCache1.TryGetValue(type, out var query))
        {
            query = new EcsQuery<T1>(this);
            _queryCache1[type] = query;
        }

        return (EcsQuery<T1>)query;
    }

    public EcsQuery<T1, T2> Query<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        var key = (typeof(T1), typeof(T2));
        if (!_queryCache2.TryGetValue(key, out var query))
        {
            query = new EcsQuery<T1, T2>(this);
            _queryCache2[key] = query;
        }

        return (EcsQuery<T1, T2>)query;
    }

    public EcsQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var key = (typeof(T1), typeof(T2), typeof(T3));
        if (!_queryCache3.TryGetValue(key, out var query))
        {
            query = new EcsQuery<T1, T2, T3>(this);
            _queryCache3[key] = query;
        }

        return (EcsQuery<T1, T2, T3>)query;
    }

    public void ForEach<T1>(EcsAction<T1> action) where T1 : struct => Query<T1>().ForEach(action);

    public void ForEach<T1, T2>(EcsAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
        => Query<T1, T2>().ForEach(action);

    public void ForEach<T1, T2, T3>(EcsAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        => Query<T1, T2, T3>().ForEach(action);

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
