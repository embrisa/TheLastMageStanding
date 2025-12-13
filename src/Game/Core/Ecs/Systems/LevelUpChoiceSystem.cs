using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles navigation and application of level-up choices. Rendering is done via Myra.
/// </summary>
internal sealed class LevelUpChoiceSystem : IUpdateSystem
{
    private const double InputDelay = 0.5;
    private readonly LevelUpChoiceGenerator _generator;
    private Entity? _sessionEntity;
    private EcsWorld? _world;
    private bool _subscribed;
    private bool _wasOpen;
    private double _openTimer;

    public LevelUpChoiceSystem(LevelUpChoiceGenerator generator)
    {
        _generator = generator;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => OnSessionRestarted(world));
        world.EventBus.Subscribe<LevelUpChoicePickedEvent>(OnChoicePicked);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryGetChoiceState(world, out var sessionEntity, out var state))
        {
            if (_wasOpen)
            {
                PublishClosed(world);
                _wasOpen = false;
            }
            return;
        }

        if (!state.IsOpen)
        {
            if (_wasOpen)
            {
                PublishClosed(world);
                _wasOpen = false;
            }
            return;
        }

        if (!_wasOpen)
        {
            _openTimer = 0;
            _wasOpen = true;
        }
        else
        {
            _openTimer += context.DeltaSeconds;
        }

        var canSelect = _openTimer >= InputDelay;

        EnsurePaused(world, sessionEntity);

        var choices = state.Choices;
        if (choices is null || choices.Count == 0)
        {
            CloseAndResume(world, sessionEntity, ref state);
            PublishViewModel(world, state, canSelect);
            world.SetComponent(sessionEntity, state);
            return;
        }

        var choiceCount = choices.Count;
        state.SelectedIndex = Math.Clamp(state.SelectedIndex, 0, choiceCount - 1);

        if (canSelect)
        {
            if (context.Input.MenuLeftPressed)
            {
                state.SelectedIndex = (state.SelectedIndex - 1 + choiceCount) % choiceCount;
            }

            if (context.Input.MenuRightPressed)
            {
                state.SelectedIndex = (state.SelectedIndex + 1) % choiceCount;
            }

            if (context.Input.MenuConfirmPressed)
            {
                CommitChoice(world, sessionEntity, ref state, choices[state.SelectedIndex]);
                PublishViewModel(world, state, canSelect);
                world.SetComponent(sessionEntity, state);
                return;
            }
        }

        PublishViewModel(world, state, canSelect);
        world.SetComponent(sessionEntity, state);
    }

    private void CommitChoice(EcsWorld world, Entity sessionEntity, ref LevelUpChoiceState state, LevelUpChoice choice)
    {
        ApplyChoice(world, state.Player, choice);

        if (state.PendingLevels > 0)
        {
            state.PendingLevels--;
            state.Choices = _generator.GenerateChoices(world, state.Player);
            state.SelectedIndex = 0;
            state.IsOpen = true;
            EnsurePaused(world, sessionEntity);
        }
        else
        {
            CloseAndResume(world, sessionEntity, ref state);
        }
    }

    private void OnChoicePicked(LevelUpChoicePickedEvent evt)
    {
        if (_world == null)
        {
            return;
        }

        if (!TryGetChoiceState(_world, out var sessionEntity, out var state) || !state.IsOpen)
        {
            return;
        }

        var choices = state.Choices;
        if (choices is null || choices.Count == 0)
        {
            return;
        }

        for (var i = 0; i < choices.Count; i++)
        {
            if (!string.Equals(choices[i].Id, evt.ChoiceId, StringComparison.Ordinal))
            {
                continue;
            }

            state.SelectedIndex = i;
            CommitChoice(_world, sessionEntity, ref state, choices[i]);
            _world.SetComponent(sessionEntity, state);
            
            if (state.IsOpen)
            {
                _openTimer = 0;
                PublishViewModel(_world, state, false);
            }
            else
            {
                PublishViewModel(_world, state, false);
            }
            return;
        }
    }

    private void ApplyChoice(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        switch (choice.Kind)
        {
            case LevelUpChoiceKind.StatBoost:
                ApplyStatBoost(world, player, choice);
                break;
            case LevelUpChoiceKind.SkillModifier:
                ApplySkillModifier(world, player, choice);
                break;
        }

        AppendHistory(world, choice);
    }

    private static void ApplyStatBoost(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        switch (choice.StatType)
        {
            case StatBoostType.MaxHealth:
                if (world.TryGetComponent(player, out Health health))
                {
                    var ratio = health.Ratio;
                    health.Max += choice.StatAmount;
                    health.Current = health.Max * ratio;
                    world.SetComponent(player, health);
                }
                break;
            case StatBoostType.AttackDamage:
                if (world.TryGetComponent(player, out AttackStats attackStats))
                {
                    attackStats.Damage += choice.StatAmount;
                    world.SetComponent(player, attackStats);
                }
                break;
            case StatBoostType.MoveSpeed:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.MoveSpeedAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.Armor:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.ArmorAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.Power:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.PowerAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.CritChance:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.CritChanceAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
        }

        MarkStatsDirty(world, player);
    }

    private static void ApplySkillModifier(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        if (!world.TryGetComponent(player, out LevelUpSkillModifiers modifiers))
        {
            modifiers = new LevelUpSkillModifiers();
        }

        modifiers.SkillSpecificModifiers ??= new Dictionary<SkillId, SkillModifiers>();
        modifiers.SkillSpecificModifiers.TryGetValue(choice.SkillId, out var skillMods);

        switch (choice.SkillModifierType)
        {
            case SkillModifierType.DamagePercent:
                skillMods.DamageMultiplicative *= 1f + choice.SkillModifierAmount;
                break;
            case SkillModifierType.CooldownReductionPercent:
                skillMods.CooldownReductionAdditive += choice.SkillModifierAmount;
                break;
            case SkillModifierType.AoePercent:
                skillMods.AoeRadiusMultiplicative *= 1f + choice.SkillModifierAmount;
                break;
            case SkillModifierType.ProjectileCount:
                skillMods.ProjectileCountAdditive += choice.SkillModifierIntAmount;
                break;
            case SkillModifierType.PierceCount:
                skillMods.PierceCountAdditive += choice.SkillModifierIntAmount;
                break;
            case SkillModifierType.CastSpeedPercent:
                skillMods.CastTimeReductionAdditive += choice.SkillModifierAmount;
                break;
        }

        modifiers.SkillSpecificModifiers[choice.SkillId] = skillMods;
        world.SetComponent(player, modifiers);
    }

    private static StatModifiers GetLevelUpModifiers(EcsWorld world, Entity player)
    {
        if (!world.TryGetComponent(player, out LevelUpStatModifiers levelUpMods))
        {
            levelUpMods = new LevelUpStatModifiers { Value = StatModifiers.Zero };
        }

        return levelUpMods.Value;
    }

    private static void SaveLevelUpModifiers(EcsWorld world, Entity player, StatModifiers modifiers)
    {
        world.SetComponent(player, new LevelUpStatModifiers { Value = modifiers });
    }

    private static void MarkStatsDirty(EcsWorld world, Entity player)
    {
        if (world.TryGetComponent(player, out ComputedStats computed))
        {
            computed.IsDirty = true;
            world.SetComponent(player, computed);
        }
    }

    private void AppendHistory(EcsWorld world, LevelUpChoice choice)
    {
        var sessionEntity = EnsureSessionEntity(world);
        if (!sessionEntity.HasValue)
        {
            return;
        }

        var history = world.TryGetComponent(sessionEntity.Value, out LevelUpChoiceHistory existing)
            ? existing
            : new LevelUpChoiceHistory { Selections = new List<string>() };

        history.Selections ??= new List<string>();
        history.Selections.Add(choice.Title);
        world.SetComponent(sessionEntity.Value, history);
    }

    private static void CloseAndResume(EcsWorld world, Entity sessionEntity, ref LevelUpChoiceState state)
    {
        state.IsOpen = false;
        state.SelectedIndex = 0;
        state.Choices = new List<LevelUpChoice>();
        state.PendingLevels = 0;
        world.SetComponent(sessionEntity, state);

        if (world.TryGetComponent(sessionEntity, out GameSession session) &&
            session.State == GameState.Paused)
        {
            session.State = GameState.Playing;
            world.SetComponent(sessionEntity, session);
        }
    }

    private static void EnsurePaused(EcsWorld world, Entity sessionEntity)
    {
        if (world.TryGetComponent(sessionEntity, out GameSession session) &&
            session.State != GameState.GameOver)
        {
            session.State = GameState.Paused;
            world.SetComponent(sessionEntity, session);
        }
    }

    private bool TryGetChoiceState(EcsWorld world, out Entity sessionEntity, out LevelUpChoiceState state)
    {
        sessionEntity = EnsureSessionEntity(world) ?? Entity.None;
        if (sessionEntity == Entity.None)
        {
            state = default;
            return false;
        }

        if (!world.TryGetComponent(sessionEntity, out state))
        {
            return false;
        }

        return true;
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

    private void OnSessionRestarted(EcsWorld world)
    {
        ClearUiState(world);
        ClearRunModifiers(world);
        PublishClosed(world);
        _wasOpen = false;
    }

    private void ClearUiState(EcsWorld world)
    {
        var sessionEntity = EnsureSessionEntity(world);
        if (!sessionEntity.HasValue)
        {
            return;
        }

        world.RemoveComponent<LevelUpChoiceState>(sessionEntity.Value);
        world.RemoveComponent<LevelUpChoiceHistory>(sessionEntity.Value);
    }

    private static void ClearRunModifiers(EcsWorld world)
    {
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            world.RemoveComponent<LevelUpStatModifiers>(entity);
            world.RemoveComponent<LevelUpSkillModifiers>(entity);

            if (world.TryGetComponent(entity, out ComputedStats computed))
            {
                computed.IsDirty = true;
                world.SetComponent(entity, computed);
            }
        });
    }

    private static void PublishViewModel(EcsWorld world, LevelUpChoiceState state, bool canSelect)
    {
        if (!state.IsOpen)
        {
            PublishClosed(world);
            return;
        }

        var choices = state.Choices ?? new List<LevelUpChoice>();
        var cards = new LevelUpChoiceCardViewModel[choices.Count];
        for (var i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            cards[i] = new LevelUpChoiceCardViewModel
            {
                Id = choice.Id,
                Title = choice.Title,
                Description = choice.Description,
                Kind = choice.Kind
            };
        }

        world.EventBus.Publish(new LevelUpChoiceViewModelEvent
        {
            ViewModel = new LevelUpChoiceViewModel
            {
                IsOpen = true,
                CanSelect = canSelect,
                SelectedIndex = Math.Clamp(state.SelectedIndex, 0, Math.Max(0, cards.Length - 1)),
                Choices = cards
            }
        });
    }

    private static void PublishClosed(EcsWorld world)
    {
        world.EventBus.Publish(new LevelUpChoiceViewModelEvent
        {
            ViewModel = new LevelUpChoiceViewModel
            {
                IsOpen = false,
                SelectedIndex = 0,
                Choices = Array.Empty<LevelUpChoiceCardViewModel>()
            }
        });
    }
}
