using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages VFX spawning, pooling, and lifecycle.
/// </summary>
internal sealed class VfxSystem : IUpdateSystem
{
    private readonly HashSet<string> _missingAssets = new();
    private EcsWorld _world = null!;

    public static bool EnableVfx { get; set; } = true;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<VfxSpawnEvent>(OnVfxSpawnEvent);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!EnableVfx) return;

        var deltaSeconds = context.DeltaSeconds;

        // Update active VFX lifetimes
        world.ForEach<ActiveVfx, Position>((Entity entity, ref ActiveVfx vfx, ref Position _) =>
        {
            vfx.RemainingTime -= deltaSeconds;

            if (vfx.RemainingTime <= 0f)
            {
                world.DestroyEntity(entity);
                return;
            }

            // Fade out effect
            var alpha = vfx.RemainingTime / vfx.Lifetime;
            vfx.Color = new Color(vfx.Color, alpha);
        });
    }

    private void OnVfxSpawnEvent(VfxSpawnEvent evt)
    {
        if (!EnableVfx) return;

        // Check if asset is missing (graceful degradation)
        if (_missingAssets.Contains(evt.EffectName))
        {
            return;
        }

        // For now, we don't have actual VFX assets, so we'll log once and track missing
        if (!_missingAssets.Contains(evt.EffectName))
        {
            Console.WriteLine($"[VFX] Missing asset: {evt.EffectName} at {evt.Position} (will not log again)");
            _missingAssets.Add(evt.EffectName);
        }

        // Spawn VFX entity (for future when assets exist)
        SpawnVfx(evt.EffectName, evt.Position, evt.Type, evt.ColorTint ?? Color.White);
    }

    private void SpawnVfx(string effectName, Vector2 position, VfxType type, Color color)
    {
        var entity = _world.CreateEntity();
        _world.SetComponent(entity, new Position(position));
        
        var lifetime = type switch
        {
            VfxType.Impact => 0.2f,
            VfxType.WindupFlash => 0.15f,
            VfxType.ProjectileTrail => 0.3f,
            VfxType.MuzzleFlash => 0.1f,
            _ => 0.2f
        };

        _world.SetComponent(entity, new ActiveVfx(effectName, type, lifetime, color, 1f));
    }
}
