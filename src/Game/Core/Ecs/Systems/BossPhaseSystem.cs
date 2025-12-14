using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies data-driven boss phase mechanics based on HP thresholds.
/// Minimal implementation: stat scaling + summon bursts on phase transitions.
/// </summary>
internal sealed class BossPhaseSystem : IUpdateSystem
{
    private readonly StageRegistry _stageRegistry;
    private readonly Random _random = new();

    public BossPhaseSystem(StageRegistry stageRegistry)
    {
        _stageRegistry = stageRegistry;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        world.ForEach<BossEncounter, Health, Position>(
            (Entity entity, ref BossEncounter encounter, ref Health health, ref Position position) =>
            {
                if (health.IsDead)
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out MoveSpeed moveSpeed) ||
                    !world.TryGetComponent(entity, out AttackStats attack))
                {
                    return;
                }

                var boss = _stageRegistry.GetBoss(encounter.BossId);
                if (boss == null || boss.Phases.Count == 0)
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out BossBaseStats baseStats))
                {
                    var baseProjectileDamage = 0f;
                    var baseWindup = 0f;
                    if (world.TryGetComponent(entity, out RangedAttacker ranged))
                    {
                        baseProjectileDamage = ranged.ProjectileDamage;
                        baseWindup = ranged.WindupSeconds;
                    }

                    baseStats = new BossBaseStats
                    {
                        BaseMoveSpeed = moveSpeed.Value,
                        BaseAttackCooldownSeconds = attack.CooldownSeconds,
                        BaseProjectileDamage = baseProjectileDamage,
                        BaseProjectileWindupSeconds = baseWindup
                    };
                    world.SetComponent(entity, baseStats);
                }

                if (!world.TryGetComponent(entity, out BossPhaseState phaseState))
                {
                    phaseState = new BossPhaseState(phaseIndex: 0);
                    world.SetComponent(entity, phaseState);
                }

                var healthFraction = health.Max <= 0f ? 0f : Math.Clamp(health.Current / health.Max, 0f, 1f);
                var nextPhaseIndex = ResolvePhaseIndex(boss, healthFraction);

                if (nextPhaseIndex != phaseState.PhaseIndex)
                {
                    phaseState.PhaseIndex = nextPhaseIndex;
                    world.SetComponent(entity, phaseState);

                    OnPhaseEntered(world, boss, boss.Phases[nextPhaseIndex], position.Value);
                }

                ApplyPhaseStats(world, entity, baseStats, boss.Phases[phaseState.PhaseIndex], ref moveSpeed, ref attack);
            });
    }

    private static int ResolvePhaseIndex(BossDefinition boss, float healthFraction)
    {
        var selectedIndex = 0;
        var selectedThreshold = float.MaxValue;

        for (var i = 0; i < boss.Phases.Count; i++)
        {
            var threshold = boss.Phases[i].StartsBelowHealthFraction;
            if (healthFraction <= threshold && threshold < selectedThreshold)
            {
                selectedThreshold = threshold;
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    private static void ApplyPhaseStats(
        EcsWorld world,
        Entity bossEntity,
        BossBaseStats baseStats,
        BossPhaseDefinition phase,
        ref MoveSpeed moveSpeed,
        ref AttackStats attack)
    {
        moveSpeed.Value = baseStats.BaseMoveSpeed * MathF.Max(0.2f, phase.MoveSpeedMultiplier);
        world.SetComponent(bossEntity, moveSpeed);

        attack.CooldownSeconds = MathF.Max(0.15f, baseStats.BaseAttackCooldownSeconds * MathF.Max(0.2f, phase.AttackCooldownMultiplier));
        world.SetComponent(bossEntity, attack);

        if (world.TryGetComponent(bossEntity, out RangedAttacker ranged))
        {
            if (baseStats.BaseProjectileDamage > 0f)
            {
                ranged.ProjectileDamage = baseStats.BaseProjectileDamage * MathF.Max(0.2f, phase.ProjectileDamageMultiplier);
            }

            if (baseStats.BaseProjectileWindupSeconds > 0f)
            {
                ranged.WindupSeconds = MathF.Max(0.2f, baseStats.BaseProjectileWindupSeconds * MathF.Max(0.2f, phase.AttackCooldownMultiplier));
            }

            world.SetComponent(bossEntity, ranged);
        }
    }

    private void OnPhaseEntered(EcsWorld world, BossDefinition boss, BossPhaseDefinition phase, Vector2 bossPosition)
    {
        var telegraphColor = phase.Name.Contains("Enrage", StringComparison.OrdinalIgnoreCase)
            ? new Color(255, 40, 40, 160)
            : new Color(200, 120, 255, 140);

        TelegraphSystem.SpawnTelegraph(world, bossPosition, new TelegraphData(
            duration: 0.75f,
            color: telegraphColor,
            radius: 90f,
            offset: Vector2.Zero));

        if (phase.SummonOnEnterCount <= 0 || string.IsNullOrWhiteSpace(phase.SummonArchetypeId))
        {
            return;
        }

        for (var i = 0; i < phase.SummonOnEnterCount; i++)
        {
            var angle = _random.NextSingle() * MathF.Tau;
            var distance = MathHelper.Lerp(70f, 140f, _random.NextSingle());
            var spawnPosition = bossPosition + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

            var request = world.CreateEntity();
            world.SetComponent(request, new EnemySpawnRequest(
                Position: spawnPosition,
                Archetype: EnemyWaveConfig.CreateArchetypeById(phase.SummonArchetypeId!)));
        }
    }
}
