using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Input;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages game session state, subscribes to wave and player death events,
/// handles restart input, and creates HUD notifications.
/// </summary>
internal sealed class GameSessionSystem : IUpdateSystem
{
    private readonly AudioSettingsConfig _audioSettings;
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;
    private Entity? _notificationEntity;
    public bool ExitRequested { get; private set; }

    private enum PauseMenuOption
    {
        Resume = 0,
        Restart = 1,
        ToggleMusic = 2,
        ToggleSfx = 3,
        Quit = 4,
    }

    public GameSessionSystem(AudioSettingsConfig audioSettings)
    {
        _audioSettings = audioSettings;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        world.EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryCacheSessionEntity(world))
        {
            return;
        }

        if (!world.TryGetComponent<GameSession>(_sessionEntity!.Value, out var session))
        {
            return;
        }

        var audioState = EnsureAudioSettingsState(world, _sessionEntity.Value);

        // Pause/resume toggle (Escape)
        if (session.State != GameState.GameOver && context.Input.PausePressed)
        {
            if (session.State == GameState.Playing)
            {
                session.State = GameState.Paused;
                world.SetComponent(_sessionEntity.Value, session);
                var pauseMenu = EnsurePauseMenu(world, _sessionEntity.Value);
                pauseMenu.SelectedIndex = 0;
                world.SetComponent(_sessionEntity.Value, pauseMenu);
            }
            else if (session.State == GameState.Paused)
            {
                session.State = GameState.Playing;
                world.SetComponent(_sessionEntity.Value, session);
            }
        }

        if (session.State == GameState.Paused)
        {
            HandlePauseMenu(world, context.Input, ref session, ref audioState);
            return;
        }

        // Update wave timer if playing
        if (session.State == GameState.Playing)
        {
            session.WaveTimer += context.DeltaSeconds;
            session.TimeSurvived += context.DeltaSeconds;
            world.SetComponent(_sessionEntity.Value, session);
        }

        // Handle restart input if game over
        if (session.State == GameState.GameOver)
        {
            if (context.Input.RestartPressed || context.Input.MenuConfirmPressed)
            {
                RestartSession(world, ref session);
            }
        }

        UpdateNotificationTimer(world, context.DeltaSeconds);
    }

    private bool TryCacheSessionEntity(EcsWorld world)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        return _sessionEntity.HasValue;
    }

    private AudioSettingsState EnsureAudioSettingsState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out AudioSettingsState audioState))
        {
            audioState = new AudioSettingsState(_audioSettings.MusicMuted, _audioSettings.SfxMuted);
            world.SetComponent(sessionEntity, audioState);
            _audioSettings.Apply();
            return audioState;
        }

        _audioSettings.MusicMuted = audioState.MusicMuted;
        _audioSettings.SfxMuted = audioState.SfxMuted;
        _audioSettings.Apply();
        return audioState;
    }

    private static PauseMenu EnsurePauseMenu(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out PauseMenu pauseMenu))
        {
            pauseMenu = new PauseMenu(0);
            world.SetComponent(sessionEntity, pauseMenu);
        }

        return pauseMenu;
    }

    private void HandlePauseMenu(EcsWorld world, InputState input, ref GameSession session, ref AudioSettingsState audioState)
    {
        var pauseMenu = EnsurePauseMenu(world, _sessionEntity!.Value);
        var optionCount = Enum.GetValues<PauseMenuOption>().Length;

        if (input.MenuUpPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex - 1 + optionCount) % optionCount;
        }

        if (input.MenuDownPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex + 1) % optionCount;
        }

        if (input.MenuConfirmPressed)
        {
            switch ((PauseMenuOption)pauseMenu.SelectedIndex)
            {
                case PauseMenuOption.Resume:
                    session.State = GameState.Playing;
                    world.SetComponent(_sessionEntity.Value, session);
                    break;
                case PauseMenuOption.Restart:
                    RestartSession(world, ref session);
                    pauseMenu.SelectedIndex = 0;
                    break;
                case PauseMenuOption.ToggleMusic:
                    audioState.MusicMuted = !audioState.MusicMuted;
                    world.SetComponent(_sessionEntity.Value, audioState);
                    ApplyAudioSettings(audioState);
                    break;
                case PauseMenuOption.ToggleSfx:
                    audioState.SfxMuted = !audioState.SfxMuted;
                    world.SetComponent(_sessionEntity.Value, audioState);
                    ApplyAudioSettings(audioState);
                    break;
                case PauseMenuOption.Quit:
                    ExitRequested = true;
                    break;
            }
        }

        world.SetComponent(_sessionEntity.Value, pauseMenu);
    }

    private void ApplyAudioSettings(AudioSettingsState audioState)
    {
        _audioSettings.MusicMuted = audioState.MusicMuted;
        _audioSettings.SfxMuted = audioState.SfxMuted;
        _audioSettings.Apply();
    }

    private void UpdateNotificationTimer(EcsWorld world, float deltaSeconds)
    {
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            if (world.TryGetComponent<WaveNotification>(_notificationEntity.Value, out var notification))
            {
                notification.RemainingSeconds -= deltaSeconds;
                if (notification.RemainingSeconds <= 0f)
                {
                    world.DestroyEntity(_notificationEntity.Value);
                    _notificationEntity = null;
                }
                else
                {
                    world.SetComponent(_notificationEntity.Value, notification);
                }
            }
        }
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.CurrentWave = evt.WaveIndex;
            session.WaveTimer = 0f;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create notification
            CreateNotification($"Wave {evt.WaveIndex} Started!", 2.5f);
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent evt)
    {
        CreateNotification($"Wave {evt.WaveIndex} Complete!", 2.5f);
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.EnemiesKilled++;
            _world.SetComponent(_sessionEntity.Value, session);
        }
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.State = GameState.GameOver;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create persistent game over notification
            CreateNotification("GAME OVER", float.MaxValue);
        }
    }

    private void CreateNotification(string message, float duration)
    {
        // Remove existing notification if any
        if (_notificationEntity.HasValue && _world.IsAlive(_notificationEntity.Value))
        {
            _world.DestroyEntity(_notificationEntity.Value);
        }

        // Create new notification entity
        _notificationEntity = _world.CreateEntity();
        _world.SetComponent(_notificationEntity.Value, new WaveNotification(message, duration));
    }

    private void RestartSession(EcsWorld world, ref GameSession session)
    {
        if (_sessionEntity is null)
        {
            return;
        }

        var sessionEntity = _sessionEntity.Value;

        // Clear all enemies
        var enemiesToRemove = new List<Entity>();
        world.ForEach<Faction>((Entity entity, ref Faction faction) =>
        {
            if (faction == Faction.Enemy)
            {
                enemiesToRemove.Add(entity);
            }
        });
        foreach (var entity in enemiesToRemove)
        {
            world.DestroyEntity(entity);
        }

        // Clear all XP orbs
        var orbsToRemove = new List<Entity>();
        world.ForEach<XpOrb>((Entity entity, ref XpOrb _) =>
        {
            orbsToRemove.Add(entity);
        });
        foreach (var entity in orbsToRemove)
        {
            world.DestroyEntity(entity);
        }

        // Reset player position and velocity
        world.ForEach<PlayerTag, Position, Velocity>((Entity entity, ref PlayerTag _, ref Position pos, ref Velocity vel) =>
        {
            pos.Value = System.Numerics.Vector2.Zero;
            vel.Value = System.Numerics.Vector2.Zero;
            world.SetComponent(entity, pos);
            world.SetComponent(entity, vel);
        });

        // Reset movement and attack stats to base values
        world.ForEach<PlayerTag, MoveSpeed, AttackStats>(
            (Entity entity, ref PlayerTag _, ref MoveSpeed moveSpeed, ref AttackStats attackStats) =>
            {
                moveSpeed.Value = 220f;
                attackStats.Damage = 20f;
                attackStats.CooldownTimer = 0f;

                world.SetComponent(entity, moveSpeed);
                world.SetComponent(entity, attackStats);
            });

        // Reset player health and XP to base values
        world.ForEach<PlayerTag, Health, PlayerXp>(
            (Entity entity, ref PlayerTag _, ref Health health, ref PlayerXp playerXp) =>
            {
                health.Max = 100f;
                health.Current = 100f;

                playerXp.Level = 1;
                playerXp.CurrentXp = 0;
                playerXp.XpToNextLevel = 10;

                world.SetComponent(entity, health);
                world.SetComponent(entity, playerXp);
            });

        // Reset session state
        session.State = GameState.Playing;
        session.CurrentWave = 0;
        session.WaveTimer = 0f;
        session.EnemiesKilled = 0;
        session.TimeSurvived = 0f;
        world.SetComponent(sessionEntity, session);

        // Reset pause menu selection
        if (world.TryGetComponent(sessionEntity, out PauseMenu pauseMenu))
        {
            pauseMenu.SelectedIndex = 0;
            world.SetComponent(sessionEntity, pauseMenu);
        }

        // Clear notification
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            world.DestroyEntity(_notificationEntity.Value);
            _notificationEntity = null;
        }

        world.EventBus.Publish(new SessionRestartedEvent());
    }
}
