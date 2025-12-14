using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Component to track run history UI state.
/// </summary>
internal struct RunHistoryUIState
{
    public bool IsOpen { get; set; }
    
    public RunHistoryUIState()
    {
        IsOpen = false;
    }
}

/// <summary>
/// Handles run history UI in the hub using Myra.
/// </summary>
internal sealed class RunHistoryUISystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly RunHistoryService _runHistoryService;
    private readonly SceneStateService _sceneStateService;

    private MyraRunHistoryScreen? _ui;
    private bool _queuedClose;

    public RunHistoryUISystem(
        RunHistoryService runHistoryService,
        SceneStateService sceneStateService)
    {
        _runHistoryService = runHistoryService;
        _sceneStateService = sceneStateService;
    }

    public void Dispose()
    {
        _ui = null;
    }

    public void Initialize(EcsWorld world)
    {
        var uiEntity = world.CreateEntity();
        world.SetComponent(uiEntity, new RunHistoryUIState());
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        // Fonts are loaded by UiFonts.Load(content) in other systems or globally
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_sceneStateService.IsInHub())
            return;

        Entity? uiEntity = null;
        var uiState = new RunHistoryUIState();
        world.ForEach<RunHistoryUIState>((Entity entity, ref RunHistoryUIState state) =>
        {
            uiEntity = entity;
            uiState = state;
        });

        if (!uiEntity.HasValue)
        {
            return;
        }

        if (_queuedClose)
        {
            uiState.IsOpen = false;
            _queuedClose = false;
            world.SetComponent(uiEntity.Value, uiState);
            _ui = null; // Dispose UI when closed
        }

        if (uiState.IsOpen && _ui == null)
        {
            // Open UI
            _ui = new MyraRunHistoryScreen(_runHistoryService, () => _queuedClose = true);
        }
        else if (!uiState.IsOpen && _ui != null)
        {
            // Close UI
            _ui = null;
        }
        
        // Update UI if open (for input handling if needed, though Myra handles input via Desktop)
        // MyraMenuScreenBase usually handles input in Update if needed, but Myra's Desktop handles events.
        // However, we might need to pass input to it if it's not automatic.
        // MyraMenuScreenBase doesn't seem to have Update method in the snippet I saw, but let's check.
    }

    public void DrawUi(EcsWorld world, in EcsDrawContext context)
    {
        if (_ui != null)
        {
            _ui.Render();
        }
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Not used
    }
}
