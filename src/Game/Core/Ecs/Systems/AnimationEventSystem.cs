using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Processes animation events to trigger hitboxes, VFX, and SFX based on animation playback.
/// Replaces hardcoded timing constants with animation-driven event windows.
/// </summary>
internal sealed class AnimationEventSystem : IUpdateSystem
{
    private readonly Dictionary<string, AnimationEventTrack> _eventTracks = new();
    private float _gameTime;

    public void Initialize(EcsWorld world)
    {
        // Register default animation event tracks
        RegisterDefaultEventTracks();
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _gameTime += context.DeltaSeconds;

        // Process player animation events
        ProcessPlayerAnimationEvents(world);

        // Process enemy animation events
        ProcessEnemyAnimationEvents(world);
    }

    private void ProcessPlayerAnimationEvents(EcsWorld world)
    {
        world.ForEach<PlayerAnimationState, AnimationDrivenAttack, Position>(
            (Entity entity, ref PlayerAnimationState animState, ref AnimationDrivenAttack attackConfig,
             ref Position position) =>
            {
                // Get directional config
                if (!world.TryGetComponent(entity, out DirectionalHitboxConfig dirConfig))
                {
                    return;
                }

                // Only process events during attack animations
                if (animState.ActiveClip != PlayerAnimationClip.Idle && 
                    animState.ActiveClip != PlayerAnimationClip.Run &&
                    animState.ActiveClip != PlayerAnimationClip.RunBackwards &&
                    animState.ActiveClip != PlayerAnimationClip.StrafeLeft &&
                    animState.ActiveClip != PlayerAnimationClip.StrafeRight &&
                    animState.ActiveClip != PlayerAnimationClip.Hit)
                {
                    // This is an attack animation
                    ProcessAnimationEvents(
                        world, 
                        entity, 
                        attackConfig.AttackAnimationName, 
                        animState.Timer, 
                        animState.Facing,
                        position.Value,
                        dirConfig);
                }
                else
                {
                    // Not attacking - ensure hitbox is disabled
                    if (world.TryGetComponent(entity, out AnimationEventState eventState))
                    {
                        if (eventState.HitboxActive)
                        {
                            DeactivateHitbox(world, entity);
                            eventState.HitboxActive = false;
                            world.SetComponent(entity, eventState);
                        }
                    }
                }
            });
    }

    private static void ProcessEnemyAnimationEvents(EcsWorld world)
    {
        world.ForEach<EnemyAnimationState, AnimationDrivenAttack, Position>(
            (Entity entity, ref EnemyAnimationState animState, ref AnimationDrivenAttack attackConfig,
             ref Position position) =>
            {
                // Only process events during attack animations
                // For now, enemies only have Idle/Run, so skip enemy events
                // This is prepared for future enemy attack animations
            });
    }

    private void ProcessAnimationEvents(
        EcsWorld world,
        Entity entity,
        string animationName,
        float currentTime,
        PlayerFacingDirection facing,
        Vector2 position,
        DirectionalHitboxConfig dirConfig)
    {
        if (!_eventTracks.TryGetValue(animationName, out var track))
        {
            return;
        }

        // Get or create event state
        if (!world.TryGetComponent(entity, out AnimationEventState eventState))
        {
            eventState = new AnimationEventState(0f, false);
        }

        var previousTime = eventState.PreviousTime;

        // Process events that occurred between previous and current time
        foreach (var evt in track.GetEventsInRange(previousTime, currentTime))
        {
            ProcessEvent(world, entity, evt, facing, position, dirConfig, ref eventState);
        }

        // Update state
        eventState.PreviousTime = currentTime;
        world.SetComponent(entity, eventState);
    }

    private static void ProcessEvent(
        EcsWorld world,
        Entity entity,
        AnimationEvent evt,
        PlayerFacingDirection facing,
        Vector2 position,
        DirectionalHitboxConfig dirConfig,
        ref AnimationEventState eventState)
    {
        switch (evt.Type)
        {
            case AnimationEventType.HitboxEnable:
                if (!eventState.HitboxActive)
                {
                    ActivateHitbox(world, entity, facing, position, dirConfig);
                    eventState.HitboxActive = true;
                }
                break;

            case AnimationEventType.HitboxDisable:
                if (eventState.HitboxActive)
                {
                    DeactivateHitbox(world, entity);
                    eventState.HitboxActive = false;
                }
                break;

            case AnimationEventType.VfxTrigger:
                if (!string.IsNullOrEmpty(evt.Data))
                {
                    world.EventBus.Publish(new VfxSpawnEvent(evt.Data, position, VfxType.WindupFlash));
                }
                break;

            case AnimationEventType.SfxTrigger:
                if (!string.IsNullOrEmpty(evt.Data))
                {
                    world.EventBus.Publish(new SfxPlayEvent(evt.Data, SfxCategory.Attack, position));
                }
                break;
        }
    }

    private static void ActivateHitbox(
        EcsWorld world,
        Entity entity,
        PlayerFacingDirection facing,
        Vector2 position,
        DirectionalHitboxConfig dirConfig)
    {
        // Get attack stats to determine damage
        if (!world.TryGetComponent(entity, out AttackStats attackStats))
        {
            return;
        }

        // Get faction
        if (!world.TryGetComponent(entity, out Faction faction))
        {
            return;
        }

        // Calculate directional offset
        var offset = dirConfig.GetOffsetForFacing(facing);

        // Get melee config or use defaults
        var meleeConfig = world.TryGetComponent(entity, out MeleeAttackConfig config)
            ? config
            : new MeleeAttackConfig(attackStats.Range, offset, 0.15f);

        // Update offset to use directional value
        meleeConfig.HitboxOffset = offset;

        // Create hitbox entity
        var hitboxEntity = world.CreateEntity();
        var hitboxPosition = position + offset;
        
        world.SetComponent(hitboxEntity, new Position(hitboxPosition));
        world.SetComponent(hitboxEntity, new AttackHitbox(entity, attackStats.Damage, faction, 999f)); // Long lifetime, controlled by events

        var hitboxLayer = faction == Faction.Player ? CollisionLayer.Projectile : CollisionLayer.Enemy;
        var targetLayer = faction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;

        world.SetComponent(
            hitboxEntity,
            Collider.CreateCircle(
                meleeConfig.HitboxRadius,
                hitboxLayer,
                targetLayer,
                isTrigger: true
            )
        );

        // Store reference to active hitbox
        world.SetComponent(entity, new ActiveAnimationHitbox { HitboxEntity = hitboxEntity });
    }

    private static void DeactivateHitbox(EcsWorld world, Entity entity)
    {
        if (world.TryGetComponent(entity, out ActiveAnimationHitbox activeHitbox))
        {
            // Destroy the hitbox entity
            if (world.IsAlive(activeHitbox.HitboxEntity))
            {
                world.DestroyEntity(activeHitbox.HitboxEntity);
            }

            world.RemoveComponent<ActiveAnimationHitbox>(entity);
        }
    }

    private void RegisterDefaultEventTracks()
    {
        // Player melee attack animation events
        // Assuming attack animation is around 0.35s total, enable hitbox from frame 2-6 (0.05s to 0.15s)
        var playerMeleeEvents = new List<AnimationEvent>
        {
            new AnimationEvent(AnimationEventType.VfxTrigger, 0.03f, "player_melee_windup"),
            new AnimationEvent(AnimationEventType.SfxTrigger, 0.04f, "player_melee_swing"),
            new AnimationEvent(AnimationEventType.HitboxEnable, 0.05f),
            new AnimationEvent(AnimationEventType.HitboxDisable, 0.15f),
        };
        _eventTracks["PlayerMelee"] = new AnimationEventTrack("PlayerMelee", playerMeleeEvents);

        // Example enemy melee (future)
        var enemyMeleeEvents = new List<AnimationEvent>
        {
            new AnimationEvent(AnimationEventType.VfxTrigger, 0.08f, "enemy_melee_windup"),
            new AnimationEvent(AnimationEventType.HitboxEnable, 0.1f),
            new AnimationEvent(AnimationEventType.HitboxDisable, 0.2f),
        };
        _eventTracks["EnemyMelee"] = new AnimationEventTrack("EnemyMelee", enemyMeleeEvents);
    }

    /// <summary>
    /// Register a custom animation event track.
    /// </summary>
    public void RegisterEventTrack(AnimationEventTrack track)
    {
        _eventTracks[track.AnimationName] = track;
    }
}

/// <summary>
/// Component that tracks the currently active hitbox entity spawned by animation events.
/// </summary>
internal struct ActiveAnimationHitbox
{
    public Entity HitboxEntity { get; set; }
}
