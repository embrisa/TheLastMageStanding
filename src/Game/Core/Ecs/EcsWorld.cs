using System;
using System.Collections.Generic;
using System.Linq;

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

internal sealed class ComponentPool<T> : IComponentPool where T : struct
{
    private readonly Dictionary<int, T> _components = new();

    public int Count => _components.Count;

    public void Set(Entity entity, T component) => _components[entity.Id] = component;

    public bool TryGet(Entity entity, out T component) => _components.TryGetValue(entity.Id, out component);

    public bool Remove(Entity entity) => _components.Remove(entity.Id);

    public void RemoveAllForEntity(int entityId) => _components.Remove(entityId);

    public IReadOnlyList<Entity> SnapshotEntities()
    {
        var entities = new List<Entity>(_components.Count);
        foreach (var id in _components.Keys)
        {
            entities.Add(new Entity(id));
        }

        return entities;
    }

    public IEnumerable<(Entity entity, T component)> Entries() =>
        _components.Select(pair => (new Entity(pair.Key), pair.Value));
}

internal sealed class EcsWorld
{
    private int _nextEntityId;
    private readonly HashSet<int> _alive = new();
    private readonly Dictionary<Type, IComponentPool> _componentPools = new();

    public Entity CreateEntity()
    {
        var entity = new Entity(_nextEntityId++);
        _alive.Add(entity.Id);
        return entity;
    }

    public bool IsAlive(Entity entity) => _alive.Contains(entity.Id);

    public void DestroyEntity(Entity entity)
    {
        _alive.Remove(entity.Id);

        foreach (var pool in _componentPools.Values)
        {
            pool.Remove(entity);
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
        var entities = pool1.SnapshotEntities();
        foreach (var entity in entities)
        {
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

    public void ForEach<T1, T2>(EcsAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var entities = pool1.SnapshotEntities();
        foreach (var entity in entities)
        {
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

    public void ForEach<T1, T2, T3>(EcsAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var entities = pool1.SnapshotEntities();
        foreach (var entity in entities)
        {
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
}

