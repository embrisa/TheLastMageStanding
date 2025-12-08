using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Type of animation event that can be triggered during animation playback.
/// </summary>
internal enum AnimationEventType
{
    /// <summary>
    /// Enable a hitbox at the specified time/frame.
    /// </summary>
    HitboxEnable = 0,

    /// <summary>
    /// Disable a hitbox at the specified time/frame.
    /// </summary>
    HitboxDisable = 1,

    /// <summary>
    /// Optional VFX trigger hook (future use).
    /// </summary>
    VfxTrigger = 2,

    /// <summary>
    /// Optional SFX trigger hook (future use).
    /// </summary>
    SfxTrigger = 3,
}

/// <summary>
/// Defines a single animation event tied to a specific time/frame in an animation.
/// </summary>
internal readonly struct AnimationEvent
{
    public AnimationEvent(AnimationEventType type, float timeSeconds, string? data = null)
    {
        Type = type;
        TimeSeconds = timeSeconds;
        Data = data;
    }

    /// <summary>
    /// The type of event to trigger.
    /// </summary>
    public AnimationEventType Type { get; }

    /// <summary>
    /// When this event should fire, in seconds from animation start.
    /// </summary>
    public float TimeSeconds { get; }

    /// <summary>
    /// Optional data payload (e.g., VFX/SFX asset name).
    /// </summary>
    public string? Data { get; }
}

/// <summary>
/// Collection of animation events for a specific animation clip.
/// Cached per animation to avoid per-frame allocations.
/// </summary>
internal sealed class AnimationEventTrack
{
    public AnimationEventTrack(string animationName, List<AnimationEvent> events)
    {
        AnimationName = animationName;
        Events = events;
    }

    /// <summary>
    /// Name/identifier of the animation this track is for.
    /// </summary>
    public string AnimationName { get; }

    /// <summary>
    /// Ordered list of events (sorted by time).
    /// </summary>
    public List<AnimationEvent> Events { get; }

    /// <summary>
    /// Get all events that should fire between two time points.
    /// </summary>
    public IEnumerable<AnimationEvent> GetEventsInRange(float previousTime, float currentTime)
    {
        foreach (var evt in Events)
        {
            // Handle both forward playback and looping
            if (previousTime <= currentTime)
            {
                // Normal forward playback
                if (evt.TimeSeconds > previousTime && evt.TimeSeconds <= currentTime)
                {
                    yield return evt;
                }
            }
            else
            {
                // Animation looped
                if (evt.TimeSeconds > previousTime || evt.TimeSeconds <= currentTime)
                {
                    yield return evt;
                }
            }
        }
    }
}

/// <summary>
/// Component that tracks the current animation playback state for event firing.
/// </summary>
internal struct AnimationEventState
{
    public AnimationEventState(float previousTime, bool hitboxActive)
    {
        PreviousTime = previousTime;
        HitboxActive = hitboxActive;
    }

    /// <summary>
    /// Previous animation time (for detecting event crossings).
    /// </summary>
    public float PreviousTime { get; set; }

    /// <summary>
    /// Whether a hitbox is currently active for this animation.
    /// </summary>
    public bool HitboxActive { get; set; }
}

/// <summary>
/// Configuration for directional hitbox offsets per facing direction.
/// Attached to entities that use animation-driven attacks.
/// </summary>
internal struct DirectionalHitboxConfig
{
    /// <summary>
    /// Offset for south-facing attacks.
    /// </summary>
    public Vector2 SouthOffset { get; set; }

    /// <summary>
    /// Offset for south-east-facing attacks.
    /// </summary>
    public Vector2 SouthEastOffset { get; set; }

    /// <summary>
    /// Offset for east-facing attacks.
    /// </summary>
    public Vector2 EastOffset { get; set; }

    /// <summary>
    /// Offset for north-east-facing attacks.
    /// </summary>
    public Vector2 NorthEastOffset { get; set; }

    /// <summary>
    /// Offset for north-facing attacks.
    /// </summary>
    public Vector2 NorthOffset { get; set; }

    /// <summary>
    /// Offset for north-west-facing attacks.
    /// </summary>
    public Vector2 NorthWestOffset { get; set; }

    /// <summary>
    /// Offset for west-facing attacks.
    /// </summary>
    public Vector2 WestOffset { get; set; }

    /// <summary>
    /// Offset for south-west-facing attacks.
    /// </summary>
    public Vector2 SouthWestOffset { get; set; }

    /// <summary>
    /// Get the hitbox offset for the given facing direction.
    /// </summary>
    public Vector2 GetOffsetForFacing(PlayerFacingDirection facing)
    {
        return facing switch
        {
            PlayerFacingDirection.South => SouthOffset,
            PlayerFacingDirection.SouthEast => SouthEastOffset,
            PlayerFacingDirection.East => EastOffset,
            PlayerFacingDirection.NorthEast => NorthEastOffset,
            PlayerFacingDirection.North => NorthOffset,
            PlayerFacingDirection.NorthWest => NorthWestOffset,
            PlayerFacingDirection.West => WestOffset,
            PlayerFacingDirection.SouthWest => SouthWestOffset,
            _ => Vector2.Zero,
        };
    }

    /// <summary>
    /// Create a default config with forward offsets for all directions.
    /// </summary>
    public static DirectionalHitboxConfig CreateDefault(float forwardDistance)
    {
        return new DirectionalHitboxConfig
        {
            SouthOffset = new Vector2(0, forwardDistance),
            SouthEastOffset = new Vector2(forwardDistance * 0.707f, forwardDistance * 0.707f),
            EastOffset = new Vector2(forwardDistance, 0),
            NorthEastOffset = new Vector2(forwardDistance * 0.707f, -forwardDistance * 0.707f),
            NorthOffset = new Vector2(0, -forwardDistance),
            NorthWestOffset = new Vector2(-forwardDistance * 0.707f, -forwardDistance * 0.707f),
            WestOffset = new Vector2(-forwardDistance, 0),
            SouthWestOffset = new Vector2(-forwardDistance * 0.707f, forwardDistance * 0.707f),
        };
    }
}

/// <summary>
/// Marks an entity as using animation-driven attacks with events and directional offsets.
/// </summary>
internal struct AnimationDrivenAttack
{
    public AnimationDrivenAttack(string attackAnimationName)
    {
        AttackAnimationName = attackAnimationName;
    }

    /// <summary>
    /// Name of the animation clip that triggers attack events.
    /// </summary>
    public string AttackAnimationName { get; set; }
}
