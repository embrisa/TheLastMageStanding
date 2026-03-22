using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Tests.Ecs;

public sealed class EcsWorldPerformanceTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void CachedQueryBenchmark_ReportsImprovementAgainstLegacyIterationPattern()
    {
        const int entityCount = 10000;
        const int iterations = 200;

        var world = CreateWorld(entityCount);
        var optimizedQuery = world.Query<TestMarker, TestValue, TestThird>();

        WarmUp(world, optimizedQuery);

        var optimizedVisits = 0;
        var optimizedElapsed = Measure(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                optimizedQuery.ForEach((Entity _, ref TestMarker marker, ref TestValue value, ref TestThird third) =>
                {
                    marker.Value += 1;
                    value.Value += 2;
                    third.Value += 3;
                    optimizedVisits++;
                });
            }
        });

        var legacyVisits = 0;
        var legacyElapsed = Measure(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                LegacyForEach(world, (Entity _, ref TestMarker marker, ref TestValue value, ref TestThird third) =>
                {
                    marker.Value += 1;
                    value.Value += 2;
                    third.Value += 3;
                    legacyVisits++;
                });
            }
        });

        Assert.Equal(entityCount * iterations, optimizedVisits);
        Assert.Equal(entityCount * iterations, legacyVisits);

        var improvement = legacyElapsed.TotalMilliseconds / optimizedElapsed.TotalMilliseconds;
        _output.WriteLine(
            $"Optimized cached query: {optimizedElapsed.TotalMilliseconds:F2} ms; " +
            $"legacy iteration: {legacyElapsed.TotalMilliseconds:F2} ms; " +
            $"speedup: {improvement:F2}x");
    }

    private static EcsWorld CreateWorld(int entityCount)
    {
        var world = new EcsWorld();
        for (var i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();
            world.SetComponent(entity, new TestMarker(i));
            world.SetComponent(entity, new TestValue(i * 2));
            world.SetComponent(entity, new TestThird(i * 3));
        }

        return world;
    }

    private static void WarmUp(EcsWorld world, EcsQuery<TestMarker, TestValue, TestThird> query)
    {
        query.ForEach((Entity _, ref TestMarker _, ref TestValue _, ref TestThird _) => { });
        LegacyForEach(world, (Entity _, ref TestMarker _, ref TestValue _, ref TestThird _) => { });
    }

    private static TimeSpan Measure(Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static void LegacyForEach(EcsWorld world, EcsAction<TestMarker, TestValue, TestThird> action)
    {
        var pool1 = world.GetPool<TestMarker>();
        var pool2 = world.GetPool<TestValue>();
        var pool3 = world.GetPool<TestThird>();
        var sourceCount = Math.Min(pool1.Count, Math.Min(pool2.Count, pool3.Count));
        var entityIds = new int[sourceCount];

        if (pool1.Count <= pool2.Count && pool1.Count <= pool3.Count)
        {
            pool1.CopyEntityIds(entityIds);
        }
        else if (pool2.Count <= pool3.Count)
        {
            pool2.CopyEntityIds(entityIds);
        }
        else
        {
            pool3.CopyEntityIds(entityIds);
        }

        foreach (var entityId in entityIds)
        {
            var entity = new Entity(entityId);
            if (!world.TryGetComponent(entity, out TestMarker marker) ||
                !world.TryGetComponent(entity, out TestValue value) ||
                !world.TryGetComponent(entity, out TestThird third))
            {
                continue;
            }

            action(entity, ref marker, ref value, ref third);
            if (!world.IsAlive(entity))
            {
                continue;
            }

            if (world.TryGetComponent(entity, out TestMarker _))
            {
                world.SetComponent(entity, marker);
            }

            if (world.TryGetComponent(entity, out TestValue _))
            {
                world.SetComponent(entity, value);
            }

            if (world.TryGetComponent(entity, out TestThird _))
            {
                world.SetComponent(entity, third);
            }
        }
    }

    private struct TestMarker(int value)
    {
        public int Value { get; set; } = value;
    }

    private struct TestValue(int value)
    {
        public int Value { get; set; } = value;
    }

    private struct TestThird(int value)
    {
        public int Value { get; set; } = value;
    }
}
