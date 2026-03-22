using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.Skills;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Ecs;

public sealed class StageRunResetServiceTests
{
    [Fact]
    public void RestoreDefaults_UsesConfiguredPlayerDefaultsAndClearsTransientPlayerState()
    {
        var world = CreateWorld();
        var progressionConfig = new ProgressionConfig(baseXpRequirement: 37, xpGrowthFactor: 2f);
        var playerFactory = new PlayerEntityFactory(world, progressionConfig);
        var player = playerFactory.CreatePlayer(new Vector2(25f, 40f));
        var session = world.CreateEntity();
        var sessionState = new GameSession { State = GameState.GameOver, CurrentWave = 4, EnemiesKilled = 12, TimeSurvived = 19f };
        world.SetComponent(session, sessionState);
        world.SetComponent(session, new PauseMenu(3));

        world.SetComponent(player, new Position(new Vector2(99f, 88f)));
        world.SetComponent(player, new Velocity(new Vector2(5f, -3f)));
        world.SetComponent(player, new MoveSpeed(999f));
        world.SetComponent(player, new BaseMoveSpeed(999f));
        world.SetComponent(player, new AttackStats(123f, 1.5f, 99f) { CooldownTimer = 0.8f });
        world.SetComponent(player, new Health(17f, 145f));
        world.SetComponent(player, new PlayerXp(23, 6, 999));
        world.SetComponent(player, new DashCooldown(1.25f));
        world.SetComponent(player, new DashInputBuffer { HasBufferedInput = true, TimeRemaining = 0.4f });
        world.SetComponent(player, new DashState { IsActive = true, Elapsed = 0.1f, Direction = Vector2.UnitX, IFrameActive = true });
        world.SetComponent(player, new Hurtbox { IsInvulnerable = true, InvulnerabilityEndsAt = 2f });
        world.SetComponent(player, new AnimationEventState(0.7f, true));
        world.SetComponent(player, new ActiveStatusEffects { Effects = new List<ActiveStatusEffect>() });
        world.SetComponent(player, new StatusEffectModifiers { Value = StatModifiers.Zero });
        world.SetComponent(player, new ActiveBuffs { Buffs = new List<TimedBuff> { new() { RemainingDuration = 1f } } });
        world.SetComponent(player, new ShieldActive(true, 1, 1f, Entity.None));
        world.SetComponent(player, new SkillCasting(SkillId.Fireball, 0.5f));

        var resetService = new StageRunResetService(playerFactory);
        resetService.RestoreDefaults(world, session, ref sessionState);

        Assert.True(world.TryGetComponent(player, out Position position));
        Assert.Equal(Vector2.Zero, position.Value);
        Assert.True(world.TryGetComponent(player, out Velocity velocity));
        Assert.Equal(Vector2.Zero, velocity.Value);
        Assert.True(world.TryGetComponent(player, out MoveSpeed moveSpeed));
        Assert.Equal(220f, moveSpeed.Value);
        Assert.True(world.TryGetComponent(player, out BaseMoveSpeed baseMoveSpeed));
        Assert.Equal(220f, baseMoveSpeed.Value);
        Assert.True(world.TryGetComponent(player, out AttackStats attackStats));
        Assert.Equal(20f, attackStats.Damage);
        Assert.Equal(0.35f, attackStats.CooldownSeconds, 3);
        Assert.Equal(0f, attackStats.CooldownTimer);
        Assert.True(world.TryGetComponent(player, out Health health));
        Assert.Equal(100f, health.Current);
        Assert.Equal(100f, health.Max);
        Assert.True(world.TryGetComponent(player, out PlayerXp playerXp));
        Assert.Equal(0, playerXp.CurrentXp);
        Assert.Equal(1, playerXp.Level);
        Assert.Equal(progressionConfig.CalculateXpForLevel(2), playerXp.XpToNextLevel);
        Assert.True(world.TryGetComponent(player, out DashCooldown dashCooldown));
        Assert.Equal(0f, dashCooldown.RemainingSeconds);
        Assert.True(world.TryGetComponent(player, out Hurtbox hurtbox));
        Assert.False(hurtbox.IsInvulnerable);
        Assert.Equal(0f, hurtbox.InvulnerabilityEndsAt);
        Assert.True(world.TryGetComponent(player, out AnimationEventState animationEventState));
        Assert.Equal(0f, animationEventState.PreviousTime);
        Assert.False(animationEventState.HitboxActive);
        Assert.False(world.TryGetComponent<DashState>(player, out _));
        Assert.False(world.TryGetComponent<ActiveStatusEffects>(player, out _));
        Assert.False(world.TryGetComponent<StatusEffectModifiers>(player, out _));
        Assert.False(world.TryGetComponent<ActiveBuffs>(player, out _));
        Assert.False(world.TryGetComponent<ShieldActive>(player, out _));
        Assert.False(world.TryGetComponent<SkillCasting>(player, out _));

        Assert.True(world.TryGetComponent(session, out GameSession restoredSession));
        Assert.Equal(GameState.Playing, restoredSession.State);
        Assert.Equal(0, restoredSession.CurrentWave);
        Assert.Equal(0, restoredSession.EnemiesKilled);
        Assert.Equal(0f, restoredSession.WaveTimer);
        Assert.Equal(0f, restoredSession.TimeSurvived);
        Assert.True(world.TryGetComponent(session, out PauseMenu pauseMenu));
        Assert.Equal(0, pauseMenu.SelectedIndex);
    }

    [Fact]
    public void RemoveTransientStageEntities_RemovesEnemiesAndOrbsOnly()
    {
        var world = CreateWorld();
        var player = world.CreateEntity();
        world.SetComponent(player, new PlayerTag());
        world.SetComponent(player, Faction.Player);

        var enemy = world.CreateEntity();
        world.SetComponent(enemy, Faction.Enemy);

        var orb = world.CreateEntity();
        world.SetComponent(orb, new XpOrb(3));

        var cleanup = new StageRunEntityCleanupService();
        cleanup.RemoveTransientStageEntities(world);

        Assert.True(world.IsAlive(player));
        Assert.False(world.IsAlive(enemy));
        Assert.False(world.IsAlive(orb));
    }

    [Fact]
    public void SessionResetPaths_ClearLevelUpRunStateAndPublishRestartEvent()
    {
        var world = CreateWorld();
        var progressionConfig = new ProgressionConfig(baseXpRequirement: 41, xpGrowthFactor: 2f);
        var playerFactory = new PlayerEntityFactory(world, progressionConfig);
        var player = playerFactory.CreatePlayer(Vector2.Zero);
        var sessionEntity = world.CreateEntity();
        world.SetComponent(sessionEntity, new GameSession());

        var restartEvents = 0;
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => restartEvents++);

        var levelUpSystem = new LevelUpChoiceSystem(new LevelUpChoiceGenerator(LevelUpChoiceConfig.Default, new SkillRegistry()));
        var sessionStateSystem = new SessionStateSystem(
            new StageRunEntityCleanupService(),
            new StageRunResetService(playerFactory));

        levelUpSystem.Initialize(world);
        sessionStateSystem.Initialize(world);

        world.SetComponent(sessionEntity, new LevelUpChoiceState
        {
            IsOpen = true,
            Player = player,
            PendingLevels = 1,
            Choices = new List<LevelUpChoice>()
        });
        world.SetComponent(sessionEntity, new LevelUpChoiceHistory
        {
            Selections = new List<string> { "stat:max_health" }
        });
        world.SetComponent(player, new LevelUpStatModifiers { Value = new StatModifiers { MoveSpeedAdditive = 25f } });
        world.SetComponent(player, new LevelUpSkillModifiers());
        world.SetComponent(player, new PlayerXp(12, 4, 123));

        sessionStateSystem.ResetForNewStage(world);
        ((EventBus)world.EventBus).ProcessEvents();

        Assert.Equal(1, restartEvents);
        Assert.False(world.TryGetComponent<LevelUpChoiceState>(sessionEntity, out _));
        Assert.False(world.TryGetComponent<LevelUpChoiceHistory>(sessionEntity, out _));
        Assert.False(world.TryGetComponent<LevelUpStatModifiers>(player, out _));
        Assert.False(world.TryGetComponent<LevelUpSkillModifiers>(player, out _));
        Assert.True(world.TryGetComponent(player, out PlayerXp playerXpAfterNewStage));
        Assert.Equal(progressionConfig.CalculateXpForLevel(2), playerXpAfterNewStage.XpToNextLevel);
        Assert.Equal(1, playerXpAfterNewStage.Level);

        world.SetComponent(sessionEntity, new LevelUpChoiceState
        {
            IsOpen = true,
            Player = player,
            PendingLevels = 1,
            Choices = new List<LevelUpChoice>()
        });
        world.SetComponent(player, new LevelUpStatModifiers { Value = new StatModifiers { ArmorAdditive = 10f } });

        world.EventBus.Publish(new SessionStateRequestEvent { Command = SessionStateCommand.Restart });
        ((EventBus)world.EventBus).ProcessEvents();

        Assert.Equal(2, restartEvents);
        Assert.False(world.TryGetComponent<LevelUpChoiceState>(sessionEntity, out _));
        Assert.False(world.TryGetComponent<LevelUpStatModifiers>(player, out _));
        Assert.True(world.TryGetComponent(player, out PlayerXp playerXpAfterRestart));
        Assert.Equal(1, playerXpAfterRestart.Level);
        Assert.Equal(0, playerXpAfterRestart.CurrentXp);
    }

    private static EcsWorld CreateWorld()
    {
        return new EcsWorld
        {
            EventBus = new EventBus()
        };
    }
}
