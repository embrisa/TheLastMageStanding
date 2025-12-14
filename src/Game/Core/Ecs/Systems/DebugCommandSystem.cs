using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Minimal console command reader for debug-only gameplay tweaks.
/// Runs a background reader thread and executes commands on the main thread during Update.
/// </summary>
internal sealed class DebugCommandSystem : IUpdateSystem
{
    private readonly ConcurrentQueue<string> _pending = new();
    private Thread? _readerThread;
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        _readerThread = new Thread(ReadLoop) { IsBackground = true, Name = "DebugCommandReader" };
        _readerThread.Start();
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        while (_pending.TryDequeue(out var line))
        {
            Execute(line);
        }
    }

    private void ReadLoop()
    {
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                Thread.Sleep(50);
                continue;
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            _pending.Enqueue(line);
        }
    }

    private void Execute(string commandLine)
    {
        if (!TryGetPlayer(_world, out var player))
        {
            return;
        }

        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (!parts[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (parts.Length == 1)
        {
            PrintUsage();
            return;
        }

        var verb = parts[1];
        if (verb.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            _world.RemoveComponent<ActiveStatusEffects>(player);
            _world.RemoveComponent<StatusEffectModifiers>(player);
            _world.RemoveComponent<StatusEffectVisual>(player);
            Console.WriteLine("[Debug] Cleared status effects from player.");
            return;
        }

        if (verb.Equals("apply", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length < 4)
            {
                PrintUsage();
                return;
            }

            if (!TryParseStatusType(parts[2], out var type))
            {
                Console.WriteLine($"[Debug] Unknown status type: '{parts[2]}'");
                return;
            }

            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
            {
                Console.WriteLine($"[Debug] Invalid duration: '{parts[3]}'");
                return;
            }

            var baseEffect = CreateDefaultEffect(type);
            var effect = new StatusEffectData(
                type,
                baseEffect.Potency,
                duration,
                baseEffect.TickInterval,
                baseEffect.MaxStacks,
                baseEffect.InitialStacks);

            // Publish a synthetic "damage" event (amount 0) so the normal application pipeline runs.
            var info = new DamageInfo(
                baseDamage: 0f,
                damageType: DamageType.True,
                flags: DamageFlags.None,
                source: DamageSource.Environmental,
                statusEffect: effect);

            var pos = _world.TryGetComponent(player, out Position position) ? position.Value : Vector2.Zero;
            _world.EventBus.Publish(new EntityDamagedEvent(Entity.None, player, 0f, info, pos, Faction.Neutral));
            Console.WriteLine($"[Debug] Applied {type} for {duration:F2}s to player.");
            return;
        }

        if (verb.Equals("immune", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length < 3)
            {
                PrintUsage();
                return;
            }

            if (!TryParseStatusType(parts[2], out var type))
            {
                Console.WriteLine($"[Debug] Unknown status type: '{parts[2]}'");
                return;
            }

            var immunities = _world.TryGetComponent(player, out StatusEffectImmunities existing)
                ? existing
                : new StatusEffectImmunities { Flags = StatusEffectImmunity.None };

            var flag = type switch
            {
                StatusEffectType.Burn => StatusEffectImmunity.Burn,
                StatusEffectType.Freeze => StatusEffectImmunity.Freeze,
                StatusEffectType.Slow => StatusEffectImmunity.Slow,
                StatusEffectType.Shock => StatusEffectImmunity.Shock,
                StatusEffectType.Poison => StatusEffectImmunity.Poison,
                _ => StatusEffectImmunity.None
            };

            if (flag == StatusEffectImmunity.None)
            {
                return;
            }

            immunities.Flags ^= flag;
            _world.SetComponent(player, immunities);
            Console.WriteLine($"[Debug] Immunity {type}: {(immunities.IsImmune(type) ? "ON" : "OFF")}");
            return;
        }

        if (verb.Equals("resist", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length < 4)
            {
                PrintUsage();
                return;
            }

            if (!TryParseStatusType(parts[2], out var type))
            {
                Console.WriteLine($"[Debug] Unknown status type: '{parts[2]}'");
                return;
            }

            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            {
                Console.WriteLine($"[Debug] Invalid resist amount: '{parts[3]}'");
                return;
            }

            var defense = _world.TryGetComponent(player, out DefensiveStats stats) ? stats : DefensiveStats.Default;
            switch (type)
            {
                case StatusEffectType.Burn:
                    defense.FireResist = amount;
                    break;
                case StatusEffectType.Freeze:
                case StatusEffectType.Slow:
                    defense.FrostResist = amount;
                    break;
                case StatusEffectType.Shock:
                case StatusEffectType.Poison:
                    defense.ArcaneResist = amount;
                    break;
                default:
                    break;
            }

            _world.SetComponent(player, defense);
            if (_world.TryGetComponent(player, out ComputedStats computed))
            {
                ComputedStats.MarkDirty(ref computed);
                _world.SetComponent(player, computed);
            }

            Console.WriteLine($"[Debug] Set resist for {type} mapping to {amount:F0}.");
            return;
        }

        PrintUsage();
    }

    private static StatusEffectData CreateDefaultEffect(StatusEffectType type) =>
        type switch
        {
            StatusEffectType.Burn => StatusEffectConfig.CreateBurn(),
            StatusEffectType.Freeze => StatusEffectConfig.CreateFreeze(),
            StatusEffectType.Slow => StatusEffectConfig.CreateSlow(),
            StatusEffectType.Shock => StatusEffectConfig.CreateShock(),
            StatusEffectType.Poison => StatusEffectConfig.CreatePoison(),
            _ => new StatusEffectData(StatusEffectType.None, 0f, 0f)
        };

    private static bool TryParseStatusType(string raw, out StatusEffectType type)
    {
        type = raw.ToLowerInvariant() switch
        {
            "burn" => StatusEffectType.Burn,
            "freeze" => StatusEffectType.Freeze,
            "slow" => StatusEffectType.Slow,
            "shock" => StatusEffectType.Shock,
            "poison" => StatusEffectType.Poison,
            _ => StatusEffectType.None
        };

        return type != StatusEffectType.None;
    }

    private static bool TryGetPlayer(EcsWorld world, out Entity player)
    {
        var found = Entity.None;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            found = entity;
        });
        player = found;
        return player.Id != 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("[Debug] Status commands:");
        Console.WriteLine("  status apply <type> <durationSeconds>");
        Console.WriteLine("  status clear");
        Console.WriteLine("  status immune <type>");
        Console.WriteLine("  status resist <type> <amount>");
        Console.WriteLine("  types: burn freeze slow shock poison");
    }
}
