using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal struct SkillSelectionUIState
{
    public bool IsOpen { get; set; }

    public SkillSelectionUIState()
    {
        IsOpen = false;
    }
}

/// <summary>
/// Hub-only skill selection "scene" implemented as a modal Myra overlay.
/// Pauses hub movement/input while open and persists confirmed loadout to the player profile.
/// </summary>
internal sealed class SkillSelectionUISystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly SceneStateService _sceneStateService;
    private readonly MetaProgressionManager _metaProgressionManager;
    private readonly SkillRegistry _skillRegistry;
    private readonly SkillSelectionController _controller = new();

    private MyraSkillSelectionScreen _ui = null!;
    private Entity? _uiEntity;
    private Entity? _sessionEntity;

    private bool _capturedSessionState;
    private GameState _previousSessionState;

    private KeyboardState _previousKeyboardState;

    public SkillSelectionUISystem(SceneStateService sceneStateService, MetaProgressionManager metaProgressionManager, SkillRegistry skillRegistry)
    {
        _sceneStateService = sceneStateService;
        _metaProgressionManager = metaProgressionManager;
        _skillRegistry = skillRegistry;
    }

    public void Initialize(EcsWorld world)
    {
        _uiEntity = world.CreateEntity();
        world.SetComponent(_uiEntity.Value, new SkillSelectionUIState());

        var uiSoundPlayer = new EventBusUiSoundPlayer(world.EventBus);
        _ui = new MyraSkillSelectionScreen(uiSoundPlayer);
        _ui.SkillClicked += skillId => _controller.SelectSkill(skillId);
        _ui.SlotClicked += slotIndex => _controller.SelectSlot(slotIndex);
        _ui.ClearSlotRequested += slotIndex => _controller.ClearSlot(slotIndex);
        _ui.ConfirmRequested += _controller.RequestConfirm;
        _ui.CancelRequested += _controller.RequestCancel;
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
    }

    public void Dispose()
    {
        _ui?.Dispose();
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_sceneStateService.IsInHub())
        {
            world.ForEach<SkillSelectionUIState>((Entity entity, ref SkillSelectionUIState state) =>
            {
                if (!state.IsOpen)
                {
                    return;
                }

                state.IsOpen = false;
                world.SetComponent(entity, state);
            });
            ForceClose(world, persistChanges: false);
            return;
        }

        if (_uiEntity is null || !_uiEntity.HasValue || !world.IsAlive(_uiEntity.Value))
        {
            _uiEntity = null;
            world.ForEach<SkillSelectionUIState>((Entity entity, ref SkillSelectionUIState _) => _uiEntity = entity);
        }

        if (_uiEntity is null || !_uiEntity.HasValue)
        {
            return;
        }

        if (!world.TryGetComponent(_uiEntity.Value, out SkillSelectionUIState uiState))
        {
            uiState = new SkillSelectionUIState();
        }

        if (context.Input.SkillSelectionPressed)
        {
            uiState.IsOpen = !uiState.IsOpen;
            world.SetComponent(_uiEntity.Value, uiState);
        }

        if (!uiState.IsOpen)
        {
            RestoreHubPause(world);
            _capturedSessionState = false;
            _controller.Close();
            _ui.ApplyState(default, detailSkill: null);
            _previousKeyboardState = Keyboard.GetState();
            return;
        }

        EnsureHubPaused(world);
        EnsureOpened(world);

        _ui.Update(context.GameTime);

        var keyboard = Keyboard.GetState();
        var tabPressed = keyboard.IsKeyDown(Keys.Tab) && !_previousKeyboardState.IsKeyDown(Keys.Tab);
        var deletePressed = (keyboard.IsKeyDown(Keys.Delete) && !_previousKeyboardState.IsKeyDown(Keys.Delete)) ||
                            (keyboard.IsKeyDown(Keys.Back) && !_previousKeyboardState.IsKeyDown(Keys.Back));
        _previousKeyboardState = keyboard;

        var result = _controller.Update(new SkillSelectionControllerInput(
            ToggleFocusRequested: tabPressed,
            DeleteRequested: deletePressed,
            MenuBackPressed: context.Input.MenuBackPressed,
            MenuLeftPressed: context.Input.MenuLeftPressed,
            MenuRightPressed: context.Input.MenuRightPressed,
            MenuUpPressed: context.Input.MenuUpPressed,
            MenuDownPressed: context.Input.MenuDownPressed,
            MenuConfirmPressed: context.Input.MenuConfirmPressed));

        if (result.CancelRequested)
        {
            ForceClose(world, persistChanges: false);
            uiState.IsOpen = false;
            world.SetComponent(_uiEntity.Value, uiState);
            return;
        }

        if (result.ConfirmRequested)
        {
            CommitLoadout(world);
            ForceClose(world, persistChanges: true);
            uiState.IsOpen = false;
            world.SetComponent(_uiEntity.Value, uiState);
            return;
        }

        var detailSkill = ResolveDetailSkill(result.ViewState.DetailSkillId);
        _ui.ApplyState(result.ViewState, detailSkill);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_ui.IsVisible)
        {
            return;
        }

        context.SpriteBatch.End();
        _ui.Render();
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public static void Open(EcsWorld world)
    {
        world.ForEach<SkillSelectionUIState>((Entity entity, ref SkillSelectionUIState state) =>
        {
            state.IsOpen = true;
            world.SetComponent(entity, state);
        });
    }

    private void EnsureOpened(EcsWorld world)
    {
        if (_capturedSessionState)
        {
            return;
        }

        _capturedSessionState = true;
        _previousSessionState = TryGetSessionState(world, out var state) ? state : GameState.Playing;
        _controller.Open(SkillLoadout.FromProfile(_metaProgressionManager.CurrentProfile.EquippedSkills));
    }

    private SkillDefinition? ResolveDetailSkill(SkillId detailSkillId)
    {
        return _skillRegistry.GetSkill(detailSkillId);
    }

    private void CommitLoadout(EcsWorld world)
    {
        var profile = _metaProgressionManager.CurrentProfile;
        profile.EquippedSkills = _controller.PendingLoadout.ToProfile();
        _metaProgressionManager.SaveProfile();

        world.ForEach<PlayerTag, EquippedSkills>((Entity entity, ref PlayerTag _, ref EquippedSkills equipped) =>
        {
            equipped = SkillLoadout.ApplyToEquippedSkills(equipped, _controller.PendingLoadout);
            equipped.IsLocked = false;
            world.SetComponent(entity, equipped);
        });
    }

    private void ForceClose(EcsWorld world, bool persistChanges)
    {
        _ = persistChanges;
        _capturedSessionState = false;

        RestoreHubPause(world);
        _controller.Close();
        _ui.ApplyState(default, detailSkill: null);
    }

    private void EnsureHubPaused(EcsWorld world)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity))
        {
            return;
        }

        if (!world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        if (!_capturedSessionState)
        {
            _previousSessionState = session.State;
        }

        if (session.State != GameState.Paused)
        {
            session.State = GameState.Paused;
            world.SetComponent(sessionEntity, session);
        }
    }

    private void RestoreHubPause(EcsWorld world)
    {
        if (!_capturedSessionState)
        {
            return;
        }

        if (!TryGetSessionEntity(world, out var sessionEntity))
        {
            return;
        }

        if (!world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        if (session.State != _previousSessionState)
        {
            session.State = _previousSessionState;
            world.SetComponent(sessionEntity, session);
        }
    }

    private static bool TryGetSessionState(EcsWorld world, out GameState state)
    {
        var captured = GameState.Playing;
        var found = false;
        world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            captured = session.State;
            found = true;
        });

        state = captured;
        return found;
    }

    private bool TryGetSessionEntity(EcsWorld world, out Entity sessionEntity)
    {
        if (_sessionEntity.HasValue && world.IsAlive(_sessionEntity.Value))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) => _sessionEntity = entity);

        if (_sessionEntity.HasValue)
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        sessionEntity = default;
        return false;
    }
}
