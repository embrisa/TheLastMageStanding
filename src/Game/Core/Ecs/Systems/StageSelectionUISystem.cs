using System.Linq;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Component to track stage selection UI state.
/// </summary>
internal struct StageSelectionUIState
{
    public bool IsOpen { get; set; }
    public int SelectedStageIndex { get; set; }
    public int SelectedActIndex { get; set; }
    
    public StageSelectionUIState()
    {
        IsOpen = false;
        SelectedStageIndex = 0;
        SelectedActIndex = 0;
    }
}

/// <summary>
/// Handles stage selection UI in the hub using Myra.
/// </summary>
internal sealed class StageSelectionUISystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly StageRegistry _stageRegistry;
    private readonly SceneManager _sceneManager;
    private readonly PlayerProfileService _profileService;

    private MyraStageSelectionScreen _ui = null!;
    private string? _queuedStartStageId;
    private bool _queuedClose;

    public StageSelectionUISystem(
        StageRegistry stageRegistry,
        SceneManager sceneManager,
        PlayerProfileService profileService)
    {
        _stageRegistry = stageRegistry;
        _sceneManager = sceneManager;
        _profileService = profileService;
    }

    public void Dispose()
    {
        _ui?.Dispose();
    }

    public void Initialize(EcsWorld world)
    {
        var uiEntity = world.CreateEntity();
        world.SetComponent(uiEntity, new StageSelectionUIState());

        var uiSoundPlayer = new EventBusUiSoundPlayer(world.EventBus);
        _ui = new MyraStageSelectionScreen(_stageRegistry, _profileService, uiSoundPlayer: uiSoundPlayer);
        _ui.StartRequested += stageId => _queuedStartStageId = stageId;
        _ui.BackRequested += () => _queuedClose = true;
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        Entity? uiEntity = null;
        var uiState = new StageSelectionUIState();
        world.ForEach<StageSelectionUIState>((Entity entity, ref StageSelectionUIState state) =>
        {
            uiEntity = entity;
            uiState = state;
        });

        if (!uiEntity.HasValue)
        {
            return;
        }

        // Clamp act index to available acts
        var maxActIndex = Math.Max(0, _stageRegistry.GetAllStages()
            .Select(s => s.ActNumber)
            .DefaultIfEmpty(1)
            .Max() - 1);
        uiState.SelectedActIndex = Math.Clamp(uiState.SelectedActIndex, 0, maxActIndex);

        if (_queuedClose)
        {
            uiState.IsOpen = false;
            _queuedClose = false;
        }

        if (!uiState.IsOpen)
        {
            _queuedStartStageId = null;
            if (_ui.IsVisible)
            {
                _ui.Hide();
            }

            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        if (!_ui.IsVisible)
        {
            _ui.Show(uiState.SelectedActIndex, uiState.SelectedStageIndex);
        }

        _ui.Update(context.GameTime);

        HandleNavigation(ref uiState, context);
        ProcessQueuedStart(ref uiState);

        uiState.SelectedActIndex = _ui.SelectedActIndex;
        uiState.SelectedStageIndex = _ui.SelectedStageIndex;

        world.SetComponent(uiEntity.Value, uiState);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_ui.IsVisible)
        {
            return;
        }

        // Myra manages its own SpriteBatch. End the shared batch, render Myra,
        // then restart for remaining UI systems.
        context.SpriteBatch.End();
        _ui.Render();
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    /// <summary>
    /// Opens the stage selection UI from the hub menu.
    /// </summary>
    public static void Open(EcsWorld world)
    {
        world.ForEach<StageSelectionUIState>((Entity entity, ref StageSelectionUIState state) =>
        {
            state.IsOpen = true;
            world.SetComponent(entity, state);
        });
    }

    private void HandleNavigation(ref StageSelectionUIState uiState, in EcsUpdateContext context)
    {
        var actStages = _stageRegistry.GetStagesForAct(uiState.SelectedActIndex + 1);

        if (context.Input.MenuLeftPressed)
        {
            _ui.ChangeAct(-1);
            actStages = _stageRegistry.GetStagesForAct(_ui.SelectedActIndex + 1);
        }
        else if (context.Input.MenuRightPressed)
        {
            _ui.ChangeAct(1);
            actStages = _stageRegistry.GetStagesForAct(_ui.SelectedActIndex + 1);
        }

        if (actStages.Count > 0)
        {
            if (context.Input.MenuDownPressed)
            {
                _ui.MoveSelection(1);
            }
            else if (context.Input.MenuUpPressed)
            {
                _ui.MoveSelection(-1);
            }
        }

        if (context.Input.MenuConfirmPressed)
        {
            _ui.StartSelectedStage();
        }

        if (context.Input.MenuBackPressed)
        {
            _ui.Close();
        }
    }

    private void ProcessQueuedStart(ref StageSelectionUIState uiState)
    {
        if (string.IsNullOrEmpty(_queuedStartStageId))
        {
            return;
        }

        var stage = _stageRegistry.GetStage(_queuedStartStageId);
        if (stage == null)
        {
            _queuedStartStageId = null;
            return;
        }

        var profile = _profileService.LoadProfile();
        if (!IsStageUnlocked(stage, profile))
        {
            _queuedStartStageId = null;
            return;
        }

        _sceneManager.TransitionToStage(stage.StageId);
        uiState.IsOpen = false;
        _ui.Hide();
        _queuedStartStageId = null;
        _queuedClose = false;
    }

    private static bool IsStageUnlocked(StageDefinition stage, PlayerProfile profile)
    {
        if (profile.MetaLevel < stage.RequiredMetaLevel)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(stage.RequiredPreviousStageId) &&
            !profile.CompletedStages.Contains(stage.RequiredPreviousStageId))
        {
            return false;
        }

        return true;
    }
}








