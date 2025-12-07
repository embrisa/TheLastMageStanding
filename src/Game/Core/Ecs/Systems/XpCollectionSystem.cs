using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles XP orb collection via proximity and optional magnet pull.
/// </summary>
internal sealed class XpCollectionSystem : IUpdateSystem
{
    private readonly ProgressionConfig _config;

    public XpCollectionSystem(ProgressionConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Find player position
        Vector2? playerPosition = null;
        Entity? playerEntity = null;

        world.ForEach<PlayerTag, Position>(
            (Entity entity, ref PlayerTag _, ref Position pos) =>
            {
                playerPosition = pos.Value;
                playerEntity = entity;
            });

        if (!playerPosition.HasValue || !playerEntity.HasValue)
            return;

        var player = playerEntity.Value;
        var playerPos = playerPosition.Value;

        var collectionRadiusSq = _config.OrbCollectionRadius * _config.OrbCollectionRadius;
        var magnetRadiusSq = _config.OrbMagnetRadius * _config.OrbMagnetRadius;
        var orbsToCollect = new List<Entity>();
        var deltaSeconds = context.DeltaSeconds; // Capture to avoid ref parameter issue

        // Check all orbs
        world.ForEach<XpOrb, Position, Velocity>(
            (Entity orbEntity, ref XpOrb orb, ref Position orbPos, ref Velocity orbVel) =>
            {
                var distSq = Vector2.DistanceSquared(playerPos, orbPos.Value);

                // Collection check
                if (distSq <= collectionRadiusSq)
                {
                    orbsToCollect.Add(orbEntity);
                    return;
                }

                // Magnet pull
                if (distSq <= magnetRadiusSq)
                {
                    var direction = Vector2.Normalize(playerPos - orbPos.Value);
                    orbPos.Value = Vector2.Lerp(
                        orbPos.Value,
                        playerPos,
                        _config.OrbMagnetStrength * deltaSeconds);
                    world.SetComponent(orbEntity, orbPos);
                }
            });

        // Collect orbs
        foreach (var orbEntity in orbsToCollect)
        {
            if (!world.TryGetComponent<XpOrb>(orbEntity, out var orb))
                continue;

            // Add XP to player
            if (world.TryGetComponent<PlayerXp>(player, out var playerXp))
            {
                playerXp.CurrentXp += orb.XpValue;

                // Check for level-up
                while (playerXp.CurrentXp >= playerXp.XpToNextLevel)
                {
                    playerXp.CurrentXp -= playerXp.XpToNextLevel;
                    playerXp.Level++;
                    playerXp.XpToNextLevel = _config.CalculateXpForLevel(playerXp.Level + 1);

                    // Publish level-up event
                    world.EventBus.Publish(new PlayerLeveledUpEvent(player, playerXp.Level));
                }

                world.SetComponent(player, playerXp);
            }

            // Publish collection event
            world.EventBus.Publish(new XpCollectedEvent(player, orb.XpValue));

            // Destroy orb
            world.DestroyEntity(orbEntity);
        }
    }
}
