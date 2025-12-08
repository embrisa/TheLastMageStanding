using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// System that converts player attack input into skill cast requests.
/// Bridges the existing PlayerAttackIntentEvent to the skill system.
/// </summary>
internal sealed class PlayerSkillInputSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerAttackIntentEvent>(OnPlayerAttackIntent);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // This system is event-driven, no per-frame update needed
    }

    private void OnPlayerAttackIntent(PlayerAttackIntentEvent evt)
    {
        // Get player's equipped skills
        if (!_world.TryGetComponent(evt.Player, out EquippedSkills equipped))
        {
            return;
        }

        // Get player position for targeting
        if (!_world.TryGetComponent(evt.Player, out Position position))
        {
            return;
        }

        // Get movement direction for skill targeting
        var direction = Vector2.Zero;
        if (_world.TryGetComponent(evt.Player, out InputIntent intent))
        {
            direction = intent.Movement;
        }

        // If no movement input, use last facing direction
        if (direction.LengthSquared() < 0.0001f)
        {
            // Try to get facing from velocity or animation
            if (_world.TryGetComponent(evt.Player, out Velocity velocity) && 
                velocity.Value.LengthSquared() > 0.0001f)
            {
                direction = Vector2.Normalize(velocity.Value);
            }
            else
            {
                // Default to right
                direction = new Vector2(1f, 0f);
            }
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        // Get primary skill (bound to attack button)
        var skillId = equipped.PrimarySkill;
        if (skillId == SkillId.None)
        {
            return;
        }

        // Calculate target position (for ground-targeted skills)
        var targetPosition = position.Value + direction * 200f;

        // Publish skill cast request
        _world.EventBus.Publish(new SkillCastRequestEvent(
            evt.Player,
            skillId,
            targetPosition,
            direction));
    }
}
