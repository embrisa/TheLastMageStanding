using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Detects when the player is near interactable NPCs and adds ProximityPrompt components.
/// Uses distance-based detection (no collision required).
/// </summary>
internal sealed class ProximityInteractionSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // First, remove all existing proximity prompts from the player
        world.ForEach<ProximityPrompt>(
            (Entity entity, ref ProximityPrompt _) =>
            {
                world.RemoveComponent<ProximityPrompt>(entity);
            });

        // Get player position
        Vector2? playerPos = null;
        Entity playerEntity = default;
        world.ForEach<PlayerTag, Position>(
            (Entity entity, ref PlayerTag _, ref Position position) =>
            {
                playerPos = position.Value;
                playerEntity = entity;
            });

        if (!playerPos.HasValue)
            return;

        // Check distance to all interactable NPCs
        Entity? closestNpc = null;
        float closestDistance = float.MaxValue;
        InteractionType closestType = InteractionType.None;
        Vector2 closestPosition = Vector2.Zero;

        world.ForEach<InteractionTrigger, Position>(
            (Entity npcEntity, ref InteractionTrigger trigger, ref Position npcPosition) =>
            {
                var distance = Vector2.Distance(playerPos.Value, npcPosition.Value);
                if (distance <= trigger.InteractionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNpc = npcEntity;
                    closestType = trigger.Type;
                    closestPosition = npcPosition.Value;
                }
            });

        // Add prompt to player if near an NPC
        if (closestNpc.HasValue)
        {
            world.SetComponent(playerEntity, new ProximityPrompt
            {
                InteractableEntity = closestNpc.Value,
                InteractionType = closestType,
                PromptPosition = closestPosition
            });
        }
    }
}
