using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// System handling skill cast requests, validation, cooldowns, and timing.
/// Integrates with the event bus and stat system for gated execution.
/// </summary>
internal sealed class SkillCastSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private readonly SkillRegistry _skillRegistry;
    private Vector2 _lastRequestedDirection;
    private Vector2 _lastRequestedTarget;

    public SkillCastSystem(SkillRegistry skillRegistry)
    {
        _skillRegistry = skillRegistry;
        _lastRequestedDirection = new Vector2(1f, 0f);
        _lastRequestedTarget = Vector2.Zero;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<SkillCastRequestEvent>(OnSkillCastRequest);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        // Update all cooldowns
        world.ForEach<SkillCooldowns>((Entity _, ref SkillCooldowns cooldowns) =>
        {
            var keys = new System.Collections.Generic.List<SkillId>(cooldowns.Cooldowns.Keys);
            foreach (var skillId in keys)
            {
                cooldowns.TickCooldown(skillId, deltaSeconds);
            }
        });

        // Update casting states
        world.ForEach<SkillCasting, Position>(
            (Entity caster, ref SkillCasting casting, ref Position position) =>
            {
                casting.CastTimeRemaining -= deltaSeconds;

                if (casting.IsComplete)
                {
                    // Cast completed - execute the skill
                    var direction = _lastRequestedDirection;
                    var target = _lastRequestedTarget;
                    
                    world.EventBus.Publish(new SkillCastCompletedEvent(
                        caster,
                        casting.SkillId,
                        position.Value,
                        target,
                        direction));

                    // Remove casting component
                    world.RemoveComponent<SkillCasting>(caster);
                }
            });
    }

    private void OnSkillCastRequest(SkillCastRequestEvent request)
    {
        // Cache direction and target for when cast completes
        _lastRequestedDirection = request.Direction;
        _lastRequestedTarget = request.TargetPosition;

        // Get skill definition
        var definition = _skillRegistry.GetSkill(request.SkillId);
        if (definition == null)
        {
            return;
        }

        // Validate caster exists and has required components
        if (!_world.TryGetComponent(request.Caster, out Position casterPosition) ||
            !_world.TryGetComponent(request.Caster, out SkillCooldowns cooldowns))
        {
            return;
        }

        // Check if already casting
        if (_world.TryGetComponent<SkillCasting>(request.Caster, out _))
        {
            _world.EventBus.Publish(new SkillCastCancelledEvent(
                request.Caster, 
                request.SkillId, 
                "Already casting"));
            return;
        }

        // Check cooldown
        if (cooldowns.IsOnCooldown(request.SkillId))
        {
            _world.EventBus.Publish(new SkillCastCancelledEvent(
                request.Caster, 
                request.SkillId, 
                "On cooldown"));
            return;
        }

        // Get modifiers for this skill
        var skillModifiers = SkillModifiers.Zero;
        var globalCdr = 0f;
        
        if (_world.TryGetComponent(request.Caster, out PlayerSkillModifiers playerMods))
        {
            skillModifiers = playerMods.GetModifiersForSkill(definition.Id, definition.Element);
        }

        // Get global CDR from character stats
        if (_world.TryGetComponent(request.Caster, out ComputedStats stats))
        {
            globalCdr = stats.EffectiveCooldownReduction;
        }

        // Calculate effective skill stats
        var effectiveStats = ComputedSkillStats.Calculate(definition, skillModifiers, globalCdr);

        // TODO: Check resource cost (mana/energy) once implemented
        // if (resourceCurrent < effectiveStats.EffectiveResourceCost) return;

        // Set cooldown
        cooldowns.SetCooldown(request.SkillId, effectiveStats.EffectiveCooldown);
        _world.SetComponent(request.Caster, cooldowns);

        // If skill has cast time, start casting
        if (effectiveStats.EffectiveCastTime > 0f)
        {
            _world.SetComponent(request.Caster, new SkillCasting(request.SkillId, effectiveStats.EffectiveCastTime));
            
            _world.EventBus.Publish(new SkillCastStartedEvent(
                request.Caster, 
                request.SkillId, 
                effectiveStats.EffectiveCastTime));
        }
        else
        {
            // Instant cast - execute immediately
            _world.EventBus.Publish(new SkillCastCompletedEvent(
                request.Caster,
                request.SkillId,
                casterPosition.Value,
                request.TargetPosition,
                request.Direction));
        }
    }
}
