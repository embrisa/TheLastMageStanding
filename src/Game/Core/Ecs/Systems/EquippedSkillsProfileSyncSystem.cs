using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Keeps the player's <see cref="EquippedSkills"/> in sync with the persisted profile loadout.
/// Also locks/unlocks skill changes based on the active scene.
/// </summary>
internal sealed class EquippedSkillsProfileSyncSystem : IUpdateSystem
{
    private readonly SceneStateService _sceneStateService;
    private readonly MetaProgressionManager _metaProgressionManager;
    private EcsWorld _world = null!;

    public EquippedSkillsProfileSyncSystem(SceneStateService sceneStateService, MetaProgressionManager metaProgressionManager)
    {
        _sceneStateService = sceneStateService;
        _metaProgressionManager = metaProgressionManager;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<SceneEnterEvent>(OnSceneEnter);

        // World can be created after the initial SceneEnter event (main menu -> hub),
        // so apply once on initialization as well.
        ApplyForCurrentScene();
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven.
    }

    private void OnSceneEnter(SceneEnterEvent evt)
    {
        ApplyForCurrentScene();
    }

    private void ApplyForCurrentScene()
    {
        var lockLoadout = _sceneStateService.IsInStage();
        var loadout = SkillLoadout.FromProfile(_metaProgressionManager.CurrentProfile.EquippedSkills);

        _world.ForEach<PlayerTag, EquippedSkills>((Entity entity, ref PlayerTag _, ref EquippedSkills equipped) =>
        {
            equipped = SkillLoadout.ApplyToEquippedSkills(equipped, loadout);
            equipped.IsLocked = lockLoadout;
            _world.SetComponent(entity, equipped);
        });
    }
}

