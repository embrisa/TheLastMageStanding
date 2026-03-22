using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs;

/// <summary>
/// Restores run-scoped player and session state from the configured defaults.
/// </summary>
internal sealed class StageRunResetService
{
    private readonly PlayerRunScopedDefaults _playerDefaults;

    public StageRunResetService(PlayerEntityFactory playerFactory)
    {
        _playerDefaults = playerFactory.CreateRunScopedDefaults();
    }

    public void RestoreDefaults(EcsWorld world, Entity sessionEntity, ref GameSession session)
    {
        RestorePlayers(world);

        session.State = GameState.Playing;
        session.CurrentWave = 0;
        session.WaveTimer = 0f;
        session.EnemiesKilled = 0;
        session.TimeSurvived = 0f;
        world.SetComponent(sessionEntity, session);

        if (world.TryGetComponent(sessionEntity, out PauseMenu pauseMenu))
        {
            pauseMenu.SelectedIndex = 0;
            world.SetComponent(sessionEntity, pauseMenu);
        }
    }

    private void RestorePlayers(EcsWorld world)
    {
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            world.SetComponent(entity, new Position(Vector2.Zero));
            world.SetComponent(entity, new Velocity(Vector2.Zero));
            world.SetComponent(entity, _playerDefaults.MoveSpeed);
            world.SetComponent(entity, _playerDefaults.BaseMoveSpeed);
            world.SetComponent(entity, _playerDefaults.AttackStats);
            world.SetComponent(entity, _playerDefaults.Health);
            world.SetComponent(entity, _playerDefaults.PlayerXp);
            world.SetComponent(entity, _playerDefaults.DashCooldown);
            world.SetComponent(entity, new InputIntent());
            world.SetComponent(entity, new DashInputBuffer());
            world.SetComponent(entity, _playerDefaults.Hurtbox);
            world.SetComponent(entity, _playerDefaults.AnimationEventState);

            world.RemoveComponent<DashState>(entity);
            world.RemoveComponent<Invulnerable>(entity);
            world.RemoveComponent<ActiveStatusEffects>(entity);
            world.RemoveComponent<StatusEffectModifiers>(entity);
            world.RemoveComponent<ActiveBuffs>(entity);
            world.RemoveComponent<ShieldActive>(entity);
            world.RemoveComponent<SkillCasting>(entity);
            world.SetComponent(entity, new SkillCooldowns());

            if (world.TryGetComponent(entity, out ComputedStats computed))
            {
                ComputedStats.MarkDirty(ref computed);
                world.SetComponent(entity, computed);
            }
        });
    }
}
