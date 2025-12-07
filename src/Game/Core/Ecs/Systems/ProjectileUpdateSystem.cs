using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Updates projectile lifetimes and cleans up expired or hit projectiles.
/// </summary>
internal sealed class ProjectileUpdateSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<Projectile>(
            (Entity entity, ref Projectile projectile) =>
            {
                // Destroy projectiles that have already hit something
                if (projectile.HasHit)
                {
                    world.DestroyEntity(entity);
                    return;
                }

                // Update lifetime
                projectile.LifetimeRemaining -= deltaSeconds;

                // Destroy projectiles that have expired
                if (projectile.LifetimeRemaining <= 0f)
                {
                    world.DestroyEntity(entity);
                }
            });
    }
}
