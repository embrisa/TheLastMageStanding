using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles hit-stop (brief time pause) and camera shake/nudge on impactful hits.
/// </summary>
internal sealed class HitStopSystem : IUpdateSystem
{
    private float _hitStopTimer;
    private Vector2 _cameraShakeOffset;
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;

    public static bool EnableHitStop { get; set; } = true;
    public static bool EnableCameraShake { get; set; } = true;
    public static float MaxHitStopDuration { get; set; } = 0.1f; // 100ms max

    public Vector2 CameraShakeOffset => EnableCameraShake ? _cameraShakeOffset : Vector2.Zero;

    public void Initialize(EcsWorld world)
    {
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        // Update hit-stop timer
        if (_hitStopTimer > 0f)
        {
            _hitStopTimer -= deltaSeconds;
            if (_hitStopTimer < 0f)
            {
                _hitStopTimer = 0f;
            }
        }

        // Update camera shake
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= deltaSeconds;
            
            if (_shakeTimer <= 0f)
            {
                _cameraShakeOffset = Vector2.Zero;
                _shakeTimer = 0f;
            }
            else
            {
                // Simple random shake
                var progress = 1f - (_shakeTimer / _shakeDuration);
                var currentIntensity = _shakeIntensity * (1f - progress); // Fade out
                
                _cameraShakeOffset = new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 2f * currentIntensity,
                    (Random.Shared.NextSingle() - 0.5f) * 2f * currentIntensity
                );
            }
        }
    }

    public bool IsHitStopped() => _hitStopTimer > 0f && EnableHitStop;

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        // Apply hit-stop based on damage amount
        var hitStopDuration = CalculateHitStopDuration(evt.Amount);
        if (hitStopDuration > 0f && EnableHitStop)
        {
            // Don't reset if already in hit-stop, just extend slightly
            _hitStopTimer = MathHelper.Max(_hitStopTimer, hitStopDuration);
        }

        // Apply camera shake for significant hits
        var shakeIntensity = CalculateShakeIntensity(evt.Amount);
        if (shakeIntensity > 0f && EnableCameraShake)
        {
            TriggerCameraShake(shakeIntensity, 0.15f);
        }
    }

    private static float CalculateHitStopDuration(float damage)
    {
        // Scale hit-stop with damage: 10 damage = 0.03s, 50+ damage = 0.1s
        var duration = damage * 0.002f;
        return MathHelper.Clamp(duration, 0.02f, MaxHitStopDuration);
    }

    private static float CalculateShakeIntensity(float damage)
    {
        // Scale shake with damage: 10 damage = 2 pixels, 50+ damage = 8 pixels
        return MathHelper.Clamp(damage * 0.15f, 1f, 8f);
    }

    private void TriggerCameraShake(float intensity, float duration)
    {
        // Don't reset shake if already active, just update if new is stronger
        if (intensity > _shakeIntensity)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeTimer = duration;
        }
    }
}
