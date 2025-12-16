using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;
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

    private MyraSkillSelectionScreen _ui = null!;
    private Entity? _uiEntity;
    private Entity? _sessionEntity;

    private SkillLoadout _snapshot;
    private SkillLoadout _pending;
    private SkillId? _selectedSkill;
    private int _cursorRow;
    private int _cursorCol;
    private SkillSelectionFocusArea _focusArea = SkillSelectionFocusArea.SkillGrid;
    private int _focusedSlot = 1;
    private bool _queuedConfirm;
    private bool _queuedCancel;
    private bool _capturedSessionState;
    private GameState _previousSessionState;

    private KeyboardState _previousKeyboardState;

    private static readonly SkillId[,] SkillGrid =
    {
        { SkillId.Firebolt, SkillId.ArcaneMissile, SkillId.FrostBolt },
        { SkillId.Fireball, SkillId.ArcaneBurst, SkillId.FrostNova },
        { SkillId.FlameWave, SkillId.ArcaneBarrage, SkillId.Blizzard }
    };

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
        _ui.SkillClicked += OnSkillClicked;
        _ui.SlotClicked += OnSlotClicked;
        _ui.ClearSlotRequested += OnClearSlotRequested;
        _ui.ConfirmRequested += () => _queuedConfirm = true;
        _ui.CancelRequested += () => _queuedCancel = true;
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
            _queuedConfirm = false;
            _queuedCancel = false;
            _selectedSkill = null;
            _snapshot = default;
            _pending = default;
            _ui.ApplyState(default);
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

        if (tabPressed)
        {
            _focusArea = _focusArea == SkillSelectionFocusArea.SkillGrid
                ? SkillSelectionFocusArea.Hotbar
                : SkillSelectionFocusArea.SkillGrid;
        }

        if (context.Input.MenuBackPressed)
        {
            _queuedCancel = true;
        }

        HandleKeyboardNavigation(context, deletePressed);

        if (_queuedCancel)
        {
            ForceClose(world, persistChanges: false);
            uiState.IsOpen = false;
            world.SetComponent(_uiEntity.Value, uiState);
            return;
        }

        if (_queuedConfirm)
        {
            CommitLoadout(world);
            ForceClose(world, persistChanges: true);
            uiState.IsOpen = false;
            world.SetComponent(_uiEntity.Value, uiState);
            return;
        }

        var hasChanges = _pending != _snapshot;
        var detailSkill = ResolveDetailSkill();

        _ui.ApplyState(new SkillSelectionScreenState(
            IsOpen: true,
            CursorRow: _cursorRow,
            CursorColumn: _cursorCol,
            SelectedSkill: _selectedSkill,
            FocusArea: _focusArea,
            FocusedSlot: _focusedSlot,
            Loadout: _pending,
            HasChanges: hasChanges,
            DetailSkill: detailSkill));
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

        _snapshot = SkillLoadout.FromProfile(_metaProgressionManager.CurrentProfile.EquippedSkills);
        _pending = _snapshot;
        _selectedSkill = null;
        _cursorRow = 0;
        _cursorCol = 0;
        _focusArea = SkillSelectionFocusArea.SkillGrid;
        _focusedSlot = 1;
    }

    private void HandleKeyboardNavigation(in EcsUpdateContext context, bool deletePressed)
    {
        if (_focusArea == SkillSelectionFocusArea.SkillGrid)
        {
            if (context.Input.MenuLeftPressed) _cursorCol = Math.Max(0, _cursorCol - 1);
            if (context.Input.MenuRightPressed) _cursorCol = Math.Min(2, _cursorCol + 1);
            if (context.Input.MenuUpPressed) _cursorRow = Math.Max(0, _cursorRow - 1);
            if (context.Input.MenuDownPressed) _cursorRow = Math.Min(2, _cursorRow + 1);

            if (context.Input.MenuConfirmPressed)
            {
                _selectedSkill = SkillGrid[_cursorRow, _cursorCol];
            }

            return;
        }

        if (context.Input.MenuLeftPressed) _focusedSlot = Math.Max(0, _focusedSlot - 1);
        if (context.Input.MenuRightPressed) _focusedSlot = Math.Min(4, _focusedSlot + 1);

        if (deletePressed && _focusedSlot is >= 1 and <= 4)
        {
            _pending = _pending.SetSlot(_focusedSlot, SkillId.None);
        }

        if (!context.Input.MenuConfirmPressed)
        {
            return;
        }

        if (_selectedSkill.HasValue)
        {
            EquipSelectedSkillToSlot(_focusedSlot);
        }
        else
        {
            var slotSkill = _pending.GetSlot(_focusedSlot);
            if (slotSkill != SkillId.None)
            {
                _selectedSkill = slotSkill;
            }
        }
    }

    private void OnSkillClicked(SkillId skillId)
    {
        _selectedSkill = skillId;
        _focusArea = SkillSelectionFocusArea.SkillGrid;
        TrySetCursorToSkill(skillId);
    }

    private void OnSlotClicked(int slotIndex)
    {
        _focusArea = SkillSelectionFocusArea.Hotbar;
        _focusedSlot = Math.Clamp(slotIndex, 0, 4);

        if (_selectedSkill.HasValue)
        {
            EquipSelectedSkillToSlot(_focusedSlot);
        }
    }

    private void OnClearSlotRequested(int slotIndex)
    {
        if (slotIndex is < 1 or > 4)
        {
            return;
        }

        _pending = _pending.SetSlot(slotIndex, SkillId.None);
    }

    private void EquipSelectedSkillToSlot(int slotIndex)
    {
        if (!_selectedSkill.HasValue)
        {
            return;
        }

        var selected = _selectedSkill.Value;
        if (selected == SkillId.None)
        {
            return;
        }

        if (slotIndex == 0 && selected == SkillId.None)
        {
            return;
        }

        // If the selected skill is already equipped, swap it from its old slot into the target slot.
        var existingSlot = FindSlotContaining(_pending, selected);
        if (existingSlot >= 0 && existingSlot != slotIndex)
        {
            var targetSkill = _pending.GetSlot(slotIndex);
            _pending = _pending.SetSlot(existingSlot, targetSkill);
            _pending = _pending.SetSlot(slotIndex, selected);
            return;
        }

        _pending = _pending.SetSlot(slotIndex, selected);
    }

    private static int FindSlotContaining(SkillLoadout loadout, SkillId skillId)
    {
        if (loadout.Primary == skillId) return 0;
        if (loadout.Hotkey1 == skillId) return 1;
        if (loadout.Hotkey2 == skillId) return 2;
        if (loadout.Hotkey3 == skillId) return 3;
        if (loadout.Hotkey4 == skillId) return 4;
        return -1;
    }

    private void TrySetCursorToSkill(SkillId skillId)
    {
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                if (SkillGrid[row, col] == skillId)
                {
                    _cursorRow = row;
                    _cursorCol = col;
                    return;
                }
            }
        }
    }

    private SkillDefinition? ResolveDetailSkill()
    {
        var detailSkillId = _selectedSkill ?? SkillGrid[_cursorRow, _cursorCol];
        return _skillRegistry.GetSkill(detailSkillId);
    }

    private void CommitLoadout(EcsWorld world)
    {
        var profile = _metaProgressionManager.CurrentProfile;
        profile.EquippedSkills = _pending.ToProfile();
        _metaProgressionManager.SaveProfile();

        world.ForEach<PlayerTag, EquippedSkills>((Entity entity, ref PlayerTag _, ref EquippedSkills equipped) =>
        {
            equipped = SkillLoadout.ApplyToEquippedSkills(equipped, _pending);
            equipped.IsLocked = false;
            world.SetComponent(entity, equipped);
        });
    }

    private void ForceClose(EcsWorld world, bool persistChanges)
    {
        _queuedConfirm = false;
        _queuedCancel = false;
        _capturedSessionState = false;

        RestoreHubPause(world);

        if (!persistChanges)
        {
            _pending = default;
            _snapshot = default;
            _selectedSkill = null;
        }

        _ui.ApplyState(default);
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
