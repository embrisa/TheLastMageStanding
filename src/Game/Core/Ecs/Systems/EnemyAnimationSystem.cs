using System;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class EnemyAnimationSystem : IUpdateSystem
{
    private readonly EnemyRenderSystem _enemyRenderSystem;

    public EnemyAnimationSystem(EnemyRenderSystem enemyRenderSystem)
    {
        _enemyRenderSystem = enemyRenderSystem;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<EnemyAnimationState, EnemySpriteAssets, Velocity>(
            (Entity entity, ref EnemyAnimationState state, ref EnemySpriteAssets assets, ref Velocity velocity) =>
            {
                if (!world.TryGetComponent(entity, out EnemyVisual visual))
                {
                    return;
                }

                _enemyRenderSystem.EnsureSpriteSet(world, entity, assets, visual.FrameSize);

                var isMoving = velocity.Value.LengthSquared() > 0.0001f;
                var facing = isMoving ? EnemyRenderSystem.ToFacing(velocity.Value) : state.Facing;
                var clip = isMoving ? EnemyAnimationClip.Run : EnemyAnimationClip.Idle;

                if (clip != state.ActiveClip)
                {
                    state.ActiveClip = clip;
                    state.FrameIndex = 0;
                    state.Timer = 0f;
                }

                state.IsMoving = isMoving;
                state.Facing = facing;

                if (!world.TryGetComponent(entity, out EnemySpriteSet spriteSet))
                {
                    return;
                }

                var animation = EnemyRenderSystem.GetAnimation(spriteSet, state.ActiveClip);
                var frameDuration = animation.FrameDurationSeconds <= 0f ? 0.1f : animation.FrameDurationSeconds;
                var frameCount = Math.Max(1, animation.Columns);

                state.Timer += deltaSeconds;
                while (state.Timer >= frameDuration)
                {
                    state.Timer -= frameDuration;
                    state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                }
            });
    }
}
