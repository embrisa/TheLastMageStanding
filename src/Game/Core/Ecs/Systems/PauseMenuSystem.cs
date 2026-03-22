using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles pause menu input/state and publishes pause view models.
/// </summary>
internal sealed class PauseMenuSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;

    public bool ExitRequested { get; private set; }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PauseMenuActionRequestedEvent>(OnPauseMenuActionRequested);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity) ||
            !world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        var pauseMenu = EnsurePauseMenu(world, sessionEntity);
        var settingsMenu = RequireSettingsMenuState(world, sessionEntity);
        var audioState = RequireAudioSettingsState(world, sessionEntity);
        var audioMenu = RequireAudioSettingsMenu(world, sessionEntity);
        var levelUpOpen = IsLevelUpChoiceOpen(world);

        if (!levelUpOpen && session.State != GameState.GameOver && context.Input.PausePressed)
        {
            if (session.State == GameState.Playing)
            {
                pauseMenu.SelectedIndex = 0;
                world.SetComponent(sessionEntity, pauseMenu);
                world.EventBus.Publish(new SessionStateRequestEvent { Command = SessionStateCommand.Pause });
                session.State = GameState.Paused;
            }
            else if (session.State == GameState.Paused)
            {
                if (settingsMenu.IsOpen)
                {
                    settingsMenu.IsOpen = false;
                    world.SetComponent(sessionEntity, settingsMenu);
                }
                else
                {
                    world.EventBus.Publish(new SessionStateRequestEvent { Command = SessionStateCommand.Resume });
                    session.State = GameState.Playing;
                }
            }
        }

        if (session.State == GameState.Paused && !levelUpOpen && !settingsMenu.IsOpen)
        {
            HandlePauseMenuInput(world, context.Input, ref pauseMenu);
            world.SetComponent(sessionEntity, pauseMenu);
        }

        PublishPauseMenuViewModel(world, session, pauseMenu, audioState, audioMenu, levelUpOpen, settingsMenu.IsOpen);
    }

    private void OnPauseMenuActionRequested(PauseMenuActionRequestedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity) ||
            !_world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        var pauseMenu = EnsurePauseMenu(_world, sessionEntity);
        var settingsMenu = RequireSettingsMenuState(_world, sessionEntity);
        var audioMenu = RequireAudioSettingsMenu(_world, sessionEntity);

        switch (evt.Action)
        {
            case PauseMenuAction.Resume:
                _world.EventBus.Publish(new SessionStateRequestEvent { Command = SessionStateCommand.Resume });
                break;
            case PauseMenuAction.Restart:
                _world.EventBus.Publish(new SessionStateRequestEvent { Command = SessionStateCommand.Restart });
                pauseMenu.SelectedIndex = 0;
                _world.SetComponent(sessionEntity, pauseMenu);
                break;
            case PauseMenuAction.OpenSettings:
                settingsMenu.IsOpen = true;
                settingsMenu.ActiveTab = "audio";
                audioMenu.IsOpen = false;
                pauseMenu.SelectedIndex = 2;
                _world.SetComponent(sessionEntity, pauseMenu);
                _world.SetComponent(sessionEntity, settingsMenu);
                _world.SetComponent(sessionEntity, audioMenu);
                break;
            case PauseMenuAction.CloseSettings:
                settingsMenu.IsOpen = false;
                _world.SetComponent(sessionEntity, settingsMenu);
                break;
            case PauseMenuAction.Quit:
                ExitRequested = true;
                break;
        }
    }

    private static void HandlePauseMenuInput(EcsWorld world, InputState input, ref PauseMenu pauseMenu)
    {
        const int optionCount = 4;
        var selectionChanged = false;

        if (input.MenuUpPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex - 1 + optionCount) % optionCount;
            selectionChanged = true;
        }

        if (input.MenuDownPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex + 1) % optionCount;
            selectionChanged = true;
        }

        if (selectionChanged)
        {
            PlayUiHover(world);
        }

        if (!input.MenuConfirmPressed)
        {
            return;
        }

        PlayUiClick(world);
        var action = pauseMenu.SelectedIndex switch
        {
            0 => PauseMenuAction.Resume,
            1 => PauseMenuAction.Restart,
            2 => PauseMenuAction.OpenSettings,
            3 => PauseMenuAction.Quit,
            _ => PauseMenuAction.Resume
        };

        world.EventBus.Publish(new PauseMenuActionRequestedEvent { Action = action });
    }

    private static void PublishPauseMenuViewModel(
        EcsWorld world,
        GameSession session,
        PauseMenu pauseMenu,
        AudioSettingsState audioState,
        AudioSettingsMenu audioMenu,
        bool levelUpOpen,
        bool settingsOpen)
    {
        var isOpen = session.State == GameState.Paused && !levelUpOpen;
        world.EventBus.Publish(new PauseMenuViewModelEvent
        {
            ViewModel = new PauseMenuViewModel
            {
                IsOpen = isOpen,
                IsAudioOpen = audioMenu.IsOpen && isOpen,
                IsSettingsOpen = settingsOpen && isOpen,
                SelectedIndex = pauseMenu.SelectedIndex,
                AudioState = audioState,
                AudioMenu = audioMenu,
                LevelUpOpen = levelUpOpen
            }
        });
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

    private static SettingsMenuState RequireSettingsMenuState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out SettingsMenuState settingsMenu))
        {
            throw new InvalidOperationException(
                "PauseMenuSystem requires SettingsMenuSystem to initialize SettingsMenuState before pause handling.");
        }

        return settingsMenu;
    }

    private static AudioSettingsMenu RequireAudioSettingsMenu(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out AudioSettingsMenu audioMenu))
        {
            throw new InvalidOperationException(
                "PauseMenuSystem requires SettingsMenuSystem to initialize AudioSettingsMenu before pause handling.");
        }

        return audioMenu;
    }

    private static AudioSettingsState RequireAudioSettingsState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out AudioSettingsState audioState))
        {
            throw new InvalidOperationException(
                "PauseMenuSystem requires SettingsMenuSystem to initialize AudioSettingsState before pause handling.");
        }

        return audioState;
    }

    private bool TryGetSessionEntity(EcsWorld world, out Entity sessionEntity)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        sessionEntity = _sessionEntity ?? default;
        return _sessionEntity.HasValue;
    }

    private static bool IsLevelUpChoiceOpen(EcsWorld world)
    {
        var open = false;
        world.ForEach<LevelUpChoiceState>((Entity _, ref LevelUpChoiceState state) =>
        {
            if (state.IsOpen)
            {
                open = true;
            }
        });

        return open;
    }

    private static void PlayUiHover(EcsWorld world)
    {
        world.EventBus.Publish(new SfxPlayEvent("UserInterfaceOnHover", SfxCategory.UI, Vector2.Zero));
    }

    private static void PlayUiClick(EcsWorld world)
    {
        world.EventBus.Publish(new SfxPlayEvent("UserInterfaceOnClick", SfxCategory.UI, Vector2.Zero));
    }
}
