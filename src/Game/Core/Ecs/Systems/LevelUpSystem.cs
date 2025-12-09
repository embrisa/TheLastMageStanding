using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Progression;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Creates level-up choice state when the player levels up and triggers notifications.
/// </summary>
internal sealed class LevelUpSystem : IUpdateSystem
{
    private readonly LevelUpChoiceGenerator _choiceGenerator;
    private EcsWorld? _world;
    private Entity? _sessionEntity;

    public LevelUpSystem(LevelUpChoiceGenerator choiceGenerator)
    {
        _choiceGenerator = choiceGenerator;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven system, no per-frame update needed
    }

    private void OnPlayerLeveledUp(PlayerLeveledUpEvent evt)
    {
        if (_world == null)
            return;

        var sessionEntity = EnsureSessionEntity(_world);
        if (!sessionEntity.HasValue)
            return;

        var state = _world.TryGetComponent(sessionEntity.Value, out LevelUpChoiceState choiceState)
            ? choiceState
            : new LevelUpChoiceState
            {
                Player = evt.Player,
                Choices = new List<LevelUpChoice>(),
                SelectedIndex = 0,
                PendingLevels = 0,
                IsOpen = false
            };

        if (state.IsOpen)
        {
            state.PendingLevels++;
            _world.SetComponent(sessionEntity.Value, state);
            CreateLevelUpNotification(evt.NewLevel);
            return;
        }

        state.Player = evt.Player;
        state.PendingLevels = 0;
        state.SelectedIndex = 0;
        state.IsOpen = true;
        state.Choices = _choiceGenerator.GenerateChoices(_world, evt.Player);

        _world.SetComponent(sessionEntity.Value, state);

        // Pause gameplay while the player is choosing
        if (_world.TryGetComponent(sessionEntity.Value, out GameSession session))
        {
            session.State = GameState.Paused;
            _world.SetComponent(sessionEntity.Value, session);
        }

        CreateLevelUpNotification(evt.NewLevel);
    }

    private Entity? EnsureSessionEntity(EcsWorld world)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            return _sessionEntity;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        return _sessionEntity;
    }

    private void CreateLevelUpNotification(int level)
    {
        if (_world == null)
            return;

        var notificationEntity = _world.CreateEntity();
        _world.SetComponent(notificationEntity, new WaveNotification(
            $"LEVEL {level}!",
            duration: 2.0f));
        _world.SetComponent(notificationEntity, new Lifetime(2.0f));
    }
}
