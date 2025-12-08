using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Config;

internal static class StatusEffectConfig
{
    // Burn
    public const float BurnDamagePerSecond = 5.0f;
    public const float BurnDuration = 3.0f;
    public const float BurnTickInterval = 0.5f;
    public const int BurnMaxStacks = 3;

    // Freeze
    public const float FreezeSlowAmount = 0.7f; // 70% slow
    public const float FreezeDuration = 2.0f;

    // Slow
    public const float SlowAmount = 0.5f; // 50% slow
    public const float SlowDuration = 1.5f;

    // Shock
    public const float ShockDamageAmp = 0.25f; // +25% damage taken
    public const float ShockDuration = 2.0f;

    // Poison
    public const float PoisonDamagePerSecond = 3.0f;
    public const float PoisonDuration = 4.0f;
    public const float PoisonTickInterval = 0.5f;
    public const int PoisonMaxStacks = 5;
    public const float PoisonRampPerStack = 0.2f;

    public static StatusEffectData CreateBurn(
        float? potency = null,
        float? duration = null,
        int? maxStacks = null,
        float? tickInterval = null)
    {
        return new StatusEffectData(
            StatusEffectType.Burn,
            potency ?? BurnDamagePerSecond,
            duration ?? BurnDuration,
            tickInterval ?? BurnTickInterval,
            maxStacks ?? BurnMaxStacks);
    }

    public static StatusEffectData CreateFreeze(
        float? potency = null,
        float? duration = null)
    {
        return new StatusEffectData(
            StatusEffectType.Freeze,
            potency ?? FreezeSlowAmount,
            duration ?? FreezeDuration,
            tickInterval: 0f,
            maxStacks: 1);
    }

    public static StatusEffectData CreateSlow(
        float? potency = null,
        float? duration = null)
    {
        return new StatusEffectData(
            StatusEffectType.Slow,
            potency ?? SlowAmount,
            duration ?? SlowDuration,
            tickInterval: 0f,
            maxStacks: 1);
    }

    public static StatusEffectData CreateShock(
        float? potency = null,
        float? duration = null)
    {
        return new StatusEffectData(
            StatusEffectType.Shock,
            potency ?? ShockDamageAmp,
            duration ?? ShockDuration,
            tickInterval: 0f,
            maxStacks: 1);
    }

    public static StatusEffectData CreatePoison(
        float? potency = null,
        float? duration = null,
        int? maxStacks = null,
        float? tickInterval = null)
    {
        return new StatusEffectData(
            StatusEffectType.Poison,
            potency ?? PoisonDamagePerSecond,
            duration ?? PoisonDuration,
            tickInterval ?? PoisonTickInterval,
            maxStacks ?? PoisonMaxStacks);
    }
}

