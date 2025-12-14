using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles stage completion (victory/death) and transitions back to hub.
/// </summary>
internal sealed class StageCompletionSystem : IUpdateSystem
{
    private readonly SceneManager _sceneManager;
    private readonly SceneStateService _sceneStateService;
    private readonly CampaignProgressionService _campaignProgressionService;
    private EcsWorld _world = null!;
    private bool _runEndedHandled;

    public StageCompletionSystem(
        SceneManager sceneManager,
        SceneStateService sceneStateService,
        CampaignProgressionService campaignProgressionService)
    {
        _sceneManager = sceneManager;
        _sceneStateService = sceneStateService;
        _campaignProgressionService = campaignProgressionService;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        world.EventBus.Subscribe<StageRunCompletedEvent>(OnStageRunCompleted);
        world.EventBus.Subscribe<SceneEnterEvent>(OnSceneEnter);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // This system mainly reacts to events
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (_runEndedHandled)
        {
            return;
        }

        _runEndedHandled = true;
        var stageId = _sceneStateService.CurrentStageId ?? string.Empty;
        _world.EventBus.Publish(new StageRunCompletedEvent(stageId, isVictory: false, bossKilled: false));
        _sceneManager.TransitionToHub();
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_runEndedHandled)
        {
            return;
        }

        if (!evt.IsBoss)
        {
            return;
        }

        if (!TryGetStageRunState(_world, out var stageState) || !stageState.IsBossStage)
        {
            return;
        }

        _world.EventBus.Publish(new StageRunCompletedEvent(stageState.StageId, isVictory: true, bossKilled: true));
    }

    private void OnStageRunCompleted(StageRunCompletedEvent evt)
    {
        if (_runEndedHandled)
        {
            return;
        }

        if (!evt.IsVictory)
        {
            return;
        }

        _runEndedHandled = true;
        var stage = _campaignProgressionService.GetStage(evt.StageId);
        if (stage != null)
        {
            if (stage.Rewards.CompletionGold > 0)
            {
                _world.EventBus.Publish(new GoldCollectedEvent(stage.Rewards.CompletionGold));
            }

            if (stage.Rewards.CompletionMetaXpBonus > 0)
            {
                _world.EventBus.Publish(new RunMetaXpBonusEvent(stage.Rewards.CompletionMetaXpBonus));
            }
        }

        _world.EventBus.Publish(new RunEndedEvent());
        _sceneManager.TransitionToHub();
    }

    private void OnSceneEnter(SceneEnterEvent evt)
    {
        // Reset flag so subsequent runs can transition back to hub again
        _runEndedHandled = false;
    }

    private static bool TryGetStageRunState(EcsWorld world, out StageRunState stageRunState)
    {
        var found = false;
        var captured = new StageRunState();
        world.ForEach<GameSession, StageRunState>((Entity _, ref GameSession _, ref StageRunState state) =>
        {
            captured = state;
            found = true;
        });

        stageRunState = captured;
        return found;
    }
}
