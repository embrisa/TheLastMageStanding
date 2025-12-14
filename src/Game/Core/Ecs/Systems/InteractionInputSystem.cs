using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles E key input to trigger interactions with nearby NPCs.
/// Opens appropriate UI based on NPC interaction type.
/// </summary>
internal sealed class InteractionInputSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!context.Input.InteractPressed)
            return;

        // Check if player has a proximity prompt (is near an interactable)
        world.ForEach<PlayerTag, ProximityPrompt>(
            (Entity _, ref PlayerTag __, ref ProximityPrompt prompt) =>
            {
                TriggerInteraction(prompt.InteractionType, world);
            });
    }

    private static void TriggerInteraction(InteractionType type, EcsWorld world)
    {
        switch (type)
        {
            case InteractionType.OpenTalentTree:
                // Simulate P key press by setting the perk tree UI state directly
                TogglePerkTreeUI(world);
                break;

            case InteractionType.OpenStageSelection:
                // Open stage selection UI
                ToggleStageSelectionUI(world);
                break;

            case InteractionType.OpenSkillSelection:
                // TODO: Open skill selection UI (future task)
                break;

            case InteractionType.OpenShop:
                // TODO: Open shop UI (future task)
                break;

            case InteractionType.OpenStats:
                ToggleRunHistoryUI(world);
                break;
        }
    }

    private static void ToggleRunHistoryUI(EcsWorld world)
    {
        // Find the RunHistoryUIState entity and toggle it
        world.ForEach<RunHistoryUIState>(
            (Entity entity, ref RunHistoryUIState state) =>
            {
                state.IsOpen = !state.IsOpen;
                world.SetComponent(entity, state);
            });
    }

    private static void TogglePerkTreeUI(EcsWorld world)
    {
        // Find the PerkTreeUI entity and toggle it
        world.ForEach<PerkTreeUI>(
            (Entity entity, ref PerkTreeUI state) =>
            {
                state.IsOpen = !state.IsOpen;
                world.SetComponent(entity, state);
            });
    }

    private static void ToggleStageSelectionUI(EcsWorld world)
    {
        // Find the StageSelectionUIState entity and toggle it
        world.ForEach<StageSelectionUIState>(
            (Entity entity, ref StageSelectionUIState state) =>
            {
                state.IsOpen = !state.IsOpen;
                world.SetComponent(entity, state);
            });
    }
}
