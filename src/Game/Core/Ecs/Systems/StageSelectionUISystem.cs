using System.Linq;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
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
    private readonly CampaignProgressionService _campaignProgressionService;
    private readonly StageSelectionController _controller = new();

    private MyraStageSelectionScreen _ui = null!;

    public StageSelectionUISystem(
        StageRegistry stageRegistry,
        SceneManager sceneManager,
        CampaignProgressionService campaignProgressionService)
    {
        _stageRegistry = stageRegistry;
        _sceneManager = sceneManager;
        _campaignProgressionService = campaignProgressionService;
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
        _ui = new MyraStageSelectionScreen(uiSoundPlayer: uiSoundPlayer);
        _ui.ActChangeRequested += delta => _controller.ChangeAct(delta, BuildCatalog());
        _ui.StageSelected += index => _controller.SelectStage(index, BuildCatalog());
        _ui.StartRequested += _controller.RequestStart;
        _ui.BackRequested += () => _controller.RequestBack();
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

        var catalog = BuildCatalog();

        if (!uiState.IsOpen)
        {
            if (_ui.IsVisible)
            {
                _ui.Hide();
            }

            _controller.Close();
            _ui.ApplyState(default);

            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        if (!_ui.IsVisible)
        {
            _controller.Open(uiState.SelectedActIndex, uiState.SelectedStageIndex, catalog);
        }

        _ui.Update(context.GameTime);
        var result = _controller.Update(catalog, new StageSelectionControllerInput(
            MoveLeft: context.Input.MenuLeftPressed,
            MoveRight: context.Input.MenuRightPressed,
            MoveUp: context.Input.MenuUpPressed,
            MoveDown: context.Input.MenuDownPressed,
            Confirm: context.Input.MenuConfirmPressed,
            Back: context.Input.MenuBackPressed));

        if (result.CloseRequested)
        {
            uiState.IsOpen = false;
            _controller.Close();
            _ui.ApplyState(default);
            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        if (!string.IsNullOrEmpty(result.StartStageId))
        {
            ProcessQueuedStart(result.StartStageId);
            uiState.IsOpen = false;
            _controller.Close();
            _ui.ApplyState(default);
            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        _ui.ApplyState(result.ViewState);
        uiState.SelectedActIndex = _controller.SelectedActIndex;
        uiState.SelectedStageIndex = _controller.SelectedStageIndex;

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

    private StageSelectionCatalog BuildCatalog()
    {
        var profile = _campaignProgressionService.LoadProfile();
        var acts = _stageRegistry.GetAllActs()
            .Select(act => new StageSelectionActOption(
                ActNumber: act.ActNumber,
                Title: act.DisplayName,
                Stages: act.Stages
                    .Select(stage => new StageSelectionStageOption(
                        StageId: stage.StageId,
                        DisplayName: stage.DisplayName,
                        Description: stage.Description,
                        IsUnlocked: CampaignProgressionService.IsStageUnlocked(stage, profile),
                        IsCompleted: CampaignProgressionService.IsStageCompleted(stage.StageId, profile),
                        LockedReason: _campaignProgressionService.GetLockReason(stage, profile)))
                    .ToArray()))
            .ToArray();

        return new StageSelectionCatalog($"Meta Level: {profile.MetaLevel}", acts);
    }

    private void ProcessQueuedStart(string? stageId)
    {
        if (string.IsNullOrEmpty(stageId))
        {
            return;
        }

        var stage = _stageRegistry.GetStage(stageId);
        if (stage == null)
        {
            return;
        }

        var profile = _campaignProgressionService.LoadProfile();
        if (!CampaignProgressionService.IsStageUnlocked(stage, profile))
        {
            return;
        }

        _sceneManager.TransitionToStage(stage.StageId);
        _ui.Hide();
    }
}



