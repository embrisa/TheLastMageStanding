using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal struct AiSeekTarget
{
    public AiSeekTarget(Faction targetFaction)
    {
        TargetFaction = targetFaction;
    }

    public Faction TargetFaction { get; set; }
}

internal enum EnemyAnimationClip
{
    Idle = 0,
    Run = 1,
}

internal readonly record struct EnemySpriteAssets(string IdleAsset, string RunAsset);

internal readonly record struct EnemyVisual(Vector2 Origin, float Scale, int FrameSize, Color Tint);

internal struct EnemySpriteSet
{
    public SpriteAnimation Idle;
    public SpriteAnimation Run;
}

internal struct EnemyAnimationState
{
    public PlayerFacingDirection Facing { get; set; }
    public EnemyAnimationClip ActiveClip { get; set; }
    public float Timer { get; set; }
    public int FrameIndex { get; set; }
    public bool IsMoving { get; set; }
}

internal readonly record struct EnemySpawnRequest(Vector2 Position, EnemyArchetype Archetype);

