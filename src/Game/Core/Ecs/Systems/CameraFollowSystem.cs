using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class CameraFollowSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var camera = context.Camera;
        world.ForEach<CameraTarget, Position>(
            (Entity _, ref CameraTarget target, ref Position position) =>
            {
                camera.LookAt(position.Value);
            });
    }
}

