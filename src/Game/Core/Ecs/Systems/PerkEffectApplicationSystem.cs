using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Perks;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies perk effects to player stats and marks ComputedStats as dirty for recalculation.
/// Listens to perk allocation/respec events to update stats immediately.
/// </summary>
internal sealed class PerkEffectApplicationSystem : IUpdateSystem
{
    private readonly PerkService _perkService;
    private EcsWorld? _world;

    public PerkEffectApplicationSystem(PerkService perkService)
    {
        _perkService = perkService;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PerkAllocatedEvent>(OnPerkAllocated);
        world.EventBus.Subscribe<PerksRespecedEvent>(OnPerksRespeced);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven, no per-frame work needed
    }

    private void OnPerkAllocated(PerkAllocatedEvent evt)
    {
        if (_world == null)
            return;

        ApplyPerksToPlayer(evt.Player);
    }

    private void OnPerksRespeced(PerksRespecedEvent evt)
    {
        if (_world == null)
            return;

        ApplyPerksToPlayer(evt.Player);
    }

    /// <summary>
    /// Recalculate and apply all perk effects to the player.
    /// </summary>
    private void ApplyPerksToPlayer(Entity player)
    {
        if (_world == null)
            return;

        // Get player's allocated perks
        if (!_world.TryGetComponent<PlayerPerks>(player, out var playerPerks))
        {
            return;
        }

        // Calculate total perk effects
        var totalEffects = _perkService.CalculateTotalEffects(playerPerks);

        // Apply stat modifiers
        if (!_world.TryGetComponent<StatModifiers>(player, out var statMods))
        {
            statMods = new StatModifiers();
        }

        // Clear perk-related modifiers (we'll recalculate from scratch)
        // Note: This assumes perks are the only source of these modifiers
        // If equipment also provides modifiers, we need a better stacking system
        statMods.PowerAdditive = totalEffects.PowerAdditive;
        statMods.PowerMultiplicative = totalEffects.PowerMultiplicative;
        statMods.AttackSpeedAdditive = totalEffects.AttackSpeedAdditive;
        statMods.AttackSpeedMultiplicative = totalEffects.AttackSpeedMultiplicative;
        statMods.CritChanceAdditive = totalEffects.CritChanceAdditive;
        statMods.CritMultiplierAdditive = totalEffects.CritMultiplierAdditive;
        statMods.CooldownReductionAdditive = totalEffects.CooldownReductionAdditive;
        statMods.ArmorAdditive = totalEffects.ArmorAdditive;
        statMods.ArcaneResistAdditive = totalEffects.ArcaneResistAdditive;
        statMods.MoveSpeedAdditive = totalEffects.MoveSpeedAdditive;
        statMods.MoveSpeedMultiplicative = totalEffects.MoveSpeedMultiplicative;

        _world.SetComponent(player, statMods);

        // Apply health bonus (modify max health directly)
        if (_world.TryGetComponent<Health>(player, out var health))
        {
            var healthDelta = totalEffects.HealthAdditive - GetPreviousHealthBonus(player);
            if (Math.Abs(healthDelta) > 0.01f)
            {
                var ratio = health.Ratio;
                health.Max += healthDelta;
                health.Current = health.Max * ratio; // Maintain health ratio
                _world.SetComponent(player, health);
                
                // Store current health bonus for next recalc
                SetPreviousHealthBonus(player, totalEffects.HealthAdditive);
            }
        }

        // Apply gameplay modifiers
        var gameplayMods = new PerkGameplayModifiers
        {
            ProjectilePierceBonus = totalEffects.ProjectilePierceBonus,
            ProjectileChainBonus = totalEffects.ProjectileChainBonus,
            DashCooldownReduction = totalEffects.DashCooldownReduction
        };
        _world.SetComponent(player, gameplayMods);

        // Mark computed stats as dirty for recalculation
        if (_world.TryGetComponent<ComputedStats>(player, out var computedStats))
        {
            computedStats.IsDirty = true;
            _world.SetComponent(player, computedStats);
        }
    }

    // Helper to track previous health bonus to avoid double-application
    private readonly Dictionary<Entity, float> _previousHealthBonuses = new();

    private float GetPreviousHealthBonus(Entity player)
    {
        return _previousHealthBonuses.TryGetValue(player, out var bonus) ? bonus : 0f;
    }

    private void SetPreviousHealthBonus(Entity player, float bonus)
    {
        _previousHealthBonuses[player] = bonus;
    }
}
