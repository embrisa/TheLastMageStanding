using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// System that converts player attack input into skill cast requests.
/// Bridges the existing PlayerAttackIntentEvent to the skill system and handles hotkey inputs (1-4).
/// Skills are aimed toward the mouse cursor position in world space.
/// </summary>
internal sealed class PlayerSkillInputSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private float _emptySlotSoundCooldown;
    private Vector2 _lastMouseWorldPosition;
    private const float EmptySlotSoundInterval = 0.5f;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerAttackIntentEvent>(OnPlayerAttackIntent);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Store mouse world position for event-based skill casts
        _lastMouseWorldPosition = context.MouseWorldPosition;
        
        // Tick down empty slot sound cooldown
        if (_emptySlotSoundCooldown > 0f)
        {
            _emptySlotSoundCooldown -= context.DeltaSeconds;
        }

        // Check for hotkey skill inputs (1-4)
        // Only process during Playing state
        GameState gameState = GameState.Playing;
        world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            gameState = session.State;
        });

        if (gameState != GameState.Playing)
        {
            return;
        }

        // Capture input state and mouse position for lambda
        var input = context.Input;
        var mouseWorldPos = context.MouseWorldPosition;

        // Find player entity and process hotkey inputs
        world.ForEach<PlayerTag, EquippedSkills>(
            (Entity entity, ref PlayerTag _, ref EquippedSkills equipped) =>
        {
            // Check each hotkey
            if (input.CastSkill1Pressed)
            {
                TryCastHotkeySkill(entity, equipped, 1, mouseWorldPos);
            }
            if (input.CastSkill2Pressed)
            {
                TryCastHotkeySkill(entity, equipped, 2, mouseWorldPos);
            }
            if (input.CastSkill3Pressed)
            {
                TryCastHotkeySkill(entity, equipped, 3, mouseWorldPos);
            }
            if (input.CastSkill4Pressed)
            {
                TryCastHotkeySkill(entity, equipped, 4, mouseWorldPos);
            }
        });
    }

    private void OnPlayerAttackIntent(PlayerAttackIntentEvent evt)
    {
        // Get player's equipped skills
        if (!_world.TryGetComponent(evt.Player, out EquippedSkills equipped))
        {
            return;
        }

        // Get primary skill (bound to attack button)
        var skillId = equipped.PrimarySkill;
        if (skillId == SkillId.None)
        {
            return;
        }

        // Cast primary skill (slot 0) using last known mouse position
        CastSkill(evt.Player, skillId, _lastMouseWorldPosition);
    }

    private void TryCastHotkeySkill(Entity entity, EquippedSkills equipped, int slotIndex, Vector2 mouseWorldPos)
    {
        var skillId = equipped.GetSkill(slotIndex);
        
        if (skillId == SkillId.None)
        {
            // Play empty slot sound if cooldown allows
            if (_emptySlotSoundCooldown <= 0f)
            {
                _world.EventBus.Publish(new SfxPlayEvent(
                    "UserInterfaceOnClick", 
                    SfxCategory.UI, 
                    Vector2.Zero, 
                    0.5f));
                _emptySlotSoundCooldown = EmptySlotSoundInterval;
            }
            return;
        }

        CastSkill(entity, skillId, mouseWorldPos);
    }

    private void CastSkill(Entity entity, SkillId skillId, Vector2 mouseWorldPos)
    {
        // Get player position for targeting
        if (!_world.TryGetComponent(entity, out Position position))
        {
            return;
        }

        // Calculate direction from player to mouse cursor
        var direction = GetTargetDirection(entity, position.Value, mouseWorldPos);

        // Use mouse cursor position as target
        var targetPosition = mouseWorldPos;

        // Publish skill cast request
        _world.EventBus.Publish(new SkillCastRequestEvent(
            entity,
            skillId,
            targetPosition,
            direction));
    }

    private Vector2 GetTargetDirection(Entity entity, Vector2 playerPosition, Vector2 mouseWorldPos)
    {
        // Calculate direction from player to mouse cursor
        var direction = mouseWorldPos - playerPosition;
        
        if (direction.LengthSquared() < 0.0001f)
        {
            // Cursor exactly on player - use movement direction as fallback
            if (_world.TryGetComponent(entity, out InputIntent intent) && 
                intent.Movement.LengthSquared() > 0.0001f)
            {
                return Vector2.Normalize(intent.Movement);
            }
            
            // Use last facing direction from velocity
            if (_world.TryGetComponent(entity, out Velocity velocity) && 
                velocity.Value.LengthSquared() > 0.0001f)
            {
                return Vector2.Normalize(velocity.Value);
            }
            
            // Default to right
            return new Vector2(1f, 0f);
        }

        return Vector2.Normalize(direction);
    }
}
