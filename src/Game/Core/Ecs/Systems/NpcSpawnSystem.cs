using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Spawns NPC interaction entities from Tiled map object markers.
/// Runs once per hub scene load.
/// </summary>
internal sealed class NpcSpawnSystem : IUpdateSystem
{
    private bool _hasSpawned;
    private readonly TiledMap _map;

    public NpcSpawnSystem(TiledMap map)
    {
        _map = map;
    }

    public void Initialize(EcsWorld world)
    {
        // Initialization not needed
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (_hasSpawned)
            return;

        System.Console.WriteLine($"[NpcSpawnSystem] Spawning NPCs from {_map.ObjectLayers.Count} object layers");

        var spawnCount = 0;
        // Spawn all NPCs from object layers
        foreach (var layer in _map.ObjectLayers)
        {
            System.Console.WriteLine($"[NpcSpawnSystem] Checking layer '{layer.Name}' with {layer.Objects.Length} objects");
            foreach (var obj in layer.Objects)
            {
                if (obj.Name is null || !obj.Name.StartsWith("npc_", StringComparison.OrdinalIgnoreCase))
                    continue;

                var interactionType = MapNpcNameToInteractionType(obj.Name);
                if (interactionType == InteractionType.None)
                {
                    System.Console.WriteLine($"[NpcSpawnSystem] Unknown NPC type: {obj.Name}");
                    continue;
                }

                // Calculate center position
                var position = obj.Position;
                if (obj.Size != MonoGame.Extended.SizeF.Empty)
                {
                    position += new Vector2(obj.Size.Width, obj.Size.Height) * 0.5f;
                }

                // Create NPC entity
                var npcEntity = world.CreateEntity();
                world.SetComponent(npcEntity, new Position { Value = position });
                world.SetComponent(npcEntity, new InteractionTrigger
                {
                    Type = interactionType,
                    InteractionRadius = 80f  // Interaction distance in pixels
                });
                
                spawnCount++;
                System.Console.WriteLine($"[NpcSpawnSystem] Spawned {obj.Name} ({interactionType}) at {position}");
            }
        }

        System.Console.WriteLine($"[NpcSpawnSystem] Spawned {spawnCount} NPCs total");
        _hasSpawned = true;
    }

    private static InteractionType MapNpcNameToInteractionType(string npcName)
    {
        return npcName.ToLowerInvariant() switch
        {
            "npc_tome_scribe" => InteractionType.OpenTalentTree,
            "npc_archivist" => InteractionType.OpenStats,
            "npc_vendor" => InteractionType.OpenShop,
            "npc_arena_master" => InteractionType.OpenStageSelection,
            "npc_ability_loadout" => InteractionType.OpenSkillSelection,
            _ => InteractionType.None
        };
    }
}
