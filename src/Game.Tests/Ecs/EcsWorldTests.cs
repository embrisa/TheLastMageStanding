using TheLastMageStanding.Game.Core.Ecs;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Ecs;

public class EcsWorldTests
{
    [Fact]
    public void ForEach_DoesNotVisitComponentsAddedDuringCurrentPass()
    {
        var world = new EcsWorld();
        var first = world.CreateEntity();
        world.SetComponent(first, new TestMarker(1));

        var visits = 0;
        world.ForEach<TestMarker>((Entity _, ref TestMarker marker) =>
        {
            visits++;
            if (marker.Value == 1)
            {
                var added = world.CreateEntity();
                world.SetComponent(added, new TestMarker(2));
            }
        });

        Assert.Equal(1, visits);

        var secondPassVisits = 0;
        world.ForEach<TestMarker>((Entity _, ref TestMarker _) => secondPassVisits++);
        Assert.Equal(2, secondPassVisits);
    }

    [Fact]
    public void ForEach_ContinuesAfterDestroyingCurrentEntity()
    {
        var world = new EcsWorld();
        for (var i = 0; i < 3; i++)
        {
            var entity = world.CreateEntity();
            world.SetComponent(entity, new TestMarker(i));
        }

        var visits = 0;
        world.ForEach<TestMarker>((Entity entity, ref TestMarker _) =>
        {
            visits++;
            if (entity.Id == 0)
            {
                world.DestroyEntity(entity);
            }
        });

        Assert.Equal(3, visits);

        var survivingMarkers = 0;
        world.ForEach<TestMarker>((Entity _, ref TestMarker _) => survivingMarkers++);
        Assert.Equal(2, survivingMarkers);
    }

    [Fact]
    public void ForEach_MultiComponentQueryUsesIntersectionAndPersistsMutations()
    {
        var world = new EcsWorld();

        var first = world.CreateEntity();
        world.SetComponent(first, new TestMarker(5));
        world.SetComponent(first, new TestValue(7));

        var second = world.CreateEntity();
        world.SetComponent(second, new TestMarker(11));
        world.SetComponent(second, new TestValue(13));

        var markerOnly = world.CreateEntity();
        world.SetComponent(markerOnly, new TestMarker(17));

        var visits = 0;
        world.ForEach<TestMarker, TestValue>((Entity _, ref TestMarker marker, ref TestValue value) =>
        {
            visits++;
            marker.Value += 1;
            value.Value += 2;
        });

        Assert.Equal(2, visits);
        Assert.True(world.TryGetComponent(first, out TestMarker firstMarker));
        Assert.True(world.TryGetComponent(first, out TestValue firstValue));
        Assert.Equal(6, firstMarker.Value);
        Assert.Equal(9, firstValue.Value);
        Assert.True(world.TryGetComponent(second, out TestMarker secondMarker));
        Assert.True(world.TryGetComponent(second, out TestValue secondValue));
        Assert.Equal(12, secondMarker.Value);
        Assert.Equal(15, secondValue.Value);
        Assert.True(world.TryGetComponent(markerOnly, out TestMarker markerOnlyValue));
        Assert.Equal(17, markerOnlyValue.Value);
    }

    [Fact]
    public void ForEach_DoesNotRestoreRemovedComponentAfterCallback()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        world.SetComponent(entity, new TestMarker(3));
        world.SetComponent(entity, new TestValue(4));

        world.ForEach<TestMarker, TestValue>((Entity current, ref TestMarker marker, ref TestValue value) =>
        {
            marker.Value = 99;
            value.Value = 42;
            world.RemoveComponent<TestMarker>(current);
        });

        Assert.False(world.TryGetComponent(entity, out TestMarker _));
        Assert.True(world.TryGetComponent(entity, out TestValue updatedValue));
        Assert.Equal(42, updatedValue.Value);
    }

    [Fact]
    public void Query_ReturnsCachedInstancePerComponentSignature()
    {
        var world = new EcsWorld();

        var singleA = world.Query<TestMarker>();
        var singleB = world.Query<TestMarker>();
        var pairA = world.Query<TestMarker, TestValue>();
        var pairB = world.Query<TestMarker, TestValue>();
        var tripleA = world.Query<TestMarker, TestValue, TestThird>();
        var tripleB = world.Query<TestMarker, TestValue, TestThird>();

        Assert.Same(singleA, singleB);
        Assert.Same(pairA, pairB);
        Assert.Same(tripleA, tripleB);
    }

    [Fact]
    public void Query_ForEach_PreservesWriteBackForEntityMovedByRemoval()
    {
        var world = new EcsWorld();

        var first = world.CreateEntity();
        world.SetComponent(first, new TestMarker(1));
        world.SetComponent(first, new TestValue(10));

        var second = world.CreateEntity();
        world.SetComponent(second, new TestMarker(2));
        world.SetComponent(second, new TestValue(20));

        world.Query<TestMarker, TestValue>().ForEach((Entity entity, ref TestMarker marker, ref TestValue value) =>
        {
            if (entity == first)
            {
                world.RemoveComponent<TestMarker>(second);
                marker.Value = 99;
                value.Value = 77;
            }
        });

        Assert.True(world.TryGetComponent(first, out TestMarker updatedMarker));
        Assert.True(world.TryGetComponent(first, out TestValue updatedValue));
        Assert.Equal(99, updatedMarker.Value);
        Assert.Equal(77, updatedValue.Value);
        Assert.False(world.TryGetComponent(second, out TestMarker _));
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
