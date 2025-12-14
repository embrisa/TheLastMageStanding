using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Spawns lightweight VFX/SFX cues for status effect lifecycle events.
/// </summary>
internal sealed class StatusEffectVfxSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<StatusEffectAppliedEvent>(OnApplied);
        world.EventBus.Subscribe<StatusEffectExpiredEvent>(OnExpired);
        world.EventBus.Subscribe<StatusEffectTickEvent>(OnTick);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven
    }

    private void OnApplied(StatusEffectAppliedEvent evt)
    {
        if (!_world.TryGetComponent(evt.Target, out Position position))
        {
            return;
        }

        var (effectName, color) = GetVfx(evt.Type);
        _world.EventBus.Publish(new VfxSpawnEvent(effectName + "_apply", position.Value, VfxType.Impact, color));
        _world.EventBus.Publish(new SfxPlayEvent($"status_{evt.Type.ToString().ToLowerInvariant()}_apply", SfxCategory.Impact, position.Value, 0.75f));
    }

    private void OnExpired(StatusEffectExpiredEvent evt)
    {
        if (!_world.TryGetComponent(evt.Target, out Position position))
        {
            return;
        }

        var (effectName, color) = GetVfx(evt.Type);
        _world.EventBus.Publish(new VfxSpawnEvent(effectName + "_expire", position.Value, VfxType.Impact, color * 0.6f));
    }

    private void OnTick(StatusEffectTickEvent evt)
    {
        if (evt.Type != StatusEffectType.Burn && evt.Type != StatusEffectType.Poison)
        {
            return;
        }

        if (GetReduceStatusEffectFlashing())
        {
            return;
        }

        if (!_world.TryGetComponent(evt.Target, out Position position))
        {
            return;
        }

        var (effectName, color) = GetVfx(evt.Type);
        _world.EventBus.Publish(new VfxSpawnEvent(effectName + "_tick", position.Value, VfxType.Impact, color * 0.8f));
    }

    private bool GetReduceStatusEffectFlashing()
    {
        var reduce = false;
        _world.ForEach<VideoSettingsState>((Entity _, ref VideoSettingsState video) =>
        {
            reduce = video.ReduceStatusEffectFlashing;
        });
        return reduce;
    }

    private static (string effectName, Color color) GetVfx(StatusEffectType type) =>
        type switch
        {
            StatusEffectType.Burn => ("status_burn", new Color(255, 120, 50)),
            StatusEffectType.Freeze => ("status_freeze", new Color(100, 200, 255)),
            StatusEffectType.Slow => ("status_slow", new Color(150, 180, 255)),
            StatusEffectType.Shock => ("status_shock", new Color(150, 100, 255)),
            StatusEffectType.Poison => ("status_poison", new Color(50, 200, 80)),
            _ => ("status_unknown", Color.White)
        };
}
