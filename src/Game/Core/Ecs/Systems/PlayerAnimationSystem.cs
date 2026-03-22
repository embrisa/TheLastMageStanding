using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class PlayerAnimationSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<PlayerAnimationState, PlayerSpriteSet, Velocity>(
            (Entity entity, ref PlayerAnimationState state, ref PlayerSpriteSet sprites, ref Velocity velocity) =>
            {
                var hitActive = false;

                if (world.TryGetComponent(entity, out PlayerHitState hitState))
                {
                    hitActive = hitState.RemainingSeconds > 0f;

                    hitState.RemainingSeconds -= deltaSeconds;
                    if (hitState.RemainingSeconds <= 0f)
                    {
                        world.RemoveComponent<PlayerHitState>(entity);
                        hitActive = false;
                    }
                    else
                    {
                        world.SetComponent(entity, hitState);
                    }
                }

                if (hitActive)
                {
                    if (world.TryGetComponent(entity, out InputIntent intent))
                    {
                        var movement = intent.Movement;
                        if (movement.LengthSquared() > 0.0001f)
                        {
                            state.Facing = PlayerRenderSystem.ToFacing(movement);
                        }
                    }

                    if (state.ActiveClip != PlayerAnimationClip.Hit)
                    {
                        state.ActiveClip = PlayerAnimationClip.Hit;
                        state.Timer = 0f;
                        state.FrameIndex = 0;
                    }

                    var hitAnimation = PlayerRenderSystem.GetAnimation(sprites, PlayerAnimationClip.Hit);
                    var frameDuration = hitAnimation.FrameDurationSeconds <= 0f ? 0.1f : hitAnimation.FrameDurationSeconds;
                    var frameCount = Math.Max(1, hitAnimation.Columns);

                    state.Timer += deltaSeconds;
                    while (state.Timer >= frameDuration)
                    {
                        state.Timer -= frameDuration;
                        state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                    }

                    state.IsMoving = false;
                }
                else
                {
                    var movement = velocity.Value;
                    var isMoving = movement.LengthSquared() > 0.0001f;
                    var facing = isMoving ? PlayerRenderSystem.ToFacing(movement) : state.Facing;
                    var clip = isMoving ? PlayerRenderSystem.ClipForFacing(facing) : PlayerAnimationClip.Idle;

                    if (clip != state.ActiveClip)
                    {
                        state.ActiveClip = clip;
                        state.FrameIndex = 0;
                        state.Timer = 0f;
                    }

                    var animation = PlayerRenderSystem.GetAnimation(sprites, state.ActiveClip);
                    var frameDuration = animation.FrameDurationSeconds <= 0f ? 0.1f : animation.FrameDurationSeconds;
                    var frameCount = Math.Max(1, animation.Columns);

                    state.Timer += deltaSeconds;
                    while (state.Timer >= frameDuration)
                    {
                        state.Timer -= frameDuration;
                        state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                    }

                    state.IsMoving = isMoving;
                    state.Facing = facing;
                }
            });
    }
}
