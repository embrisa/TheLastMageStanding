using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Player-configurable input bindings (keyboard only for now). Provides helpers to
/// fetch bindings safely and normalize persisted data.
/// </summary>
internal sealed class InputBindingsConfig
{
    public int Version { get; set; } = 1;

    public Dictionary<string, InputBinding> Bindings { get; set; } = new();

    public static InputBindingsConfig Default => CreateDefault();

    public InputBindingsConfig Clone() => new()
    {
        Version = Version,
        Bindings = Bindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
    };

    public void Normalize()
    {
        // Remove null or empty action ids; ensure every binding has a primary key.
        var normalized = new Dictionary<string, InputBinding>(StringComparer.Ordinal);
        foreach (var kvp in Bindings)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            var binding = kvp.Value;
            if (binding.Primary == Keys.None)
            {
                continue;
            }

            normalized[kvp.Key] = binding;
        }

        Bindings = normalized;
    }

    public InputBinding GetBinding(string actionId)
    {
        if (Bindings.TryGetValue(actionId, out var binding) && binding.Primary != Keys.None)
        {
            return binding;
        }

        if (Default.Bindings.TryGetValue(actionId, out var fallback))
        {
            return fallback;
        }

        return new InputBinding(Keys.None, null);
    }

    private static InputBindingsConfig CreateDefault()
    {
        var config = new InputBindingsConfig();
        config.Bindings = new Dictionary<string, InputBinding>(StringComparer.Ordinal)
        {
            [InputActions.MoveUp] = new(Keys.W, Keys.Up),
            [InputActions.MoveDown] = new(Keys.S, Keys.Down),
            [InputActions.MoveLeft] = new(Keys.A, Keys.Left),
            [InputActions.MoveRight] = new(Keys.D, Keys.Right),
            [InputActions.Pause] = new(Keys.Escape, null),

            // Menu navigation / UI
            [InputActions.MenuUp] = new(Keys.W, Keys.Up),
            [InputActions.MenuDown] = new(Keys.S, Keys.Down),
            [InputActions.MenuLeft] = new(Keys.A, Keys.Left),
            [InputActions.MenuRight] = new(Keys.D, Keys.Right),
            [InputActions.MenuConfirm] = new(Keys.Enter, Keys.Space),
            [InputActions.MenuBack] = new(Keys.Escape, Keys.Back),

            // Gameplay
            [InputActions.Attack] = new(Keys.J, null),
            [InputActions.Dash] = new(Keys.Space, Keys.LeftShift),
            [InputActions.Interact] = new(Keys.E, null),
            [InputActions.PerkTree] = new(Keys.P, null),
            [InputActions.Inventory] = new(Keys.I, null),
            [InputActions.Respec] = new(Keys.R, null),
            [InputActions.Restart] = new(Keys.R, null),

            [InputActions.Skill1] = new(Keys.D1, Keys.NumPad1),
            [InputActions.Skill2] = new(Keys.D2, Keys.NumPad2),
            [InputActions.Skill3] = new(Keys.D3, Keys.NumPad3),
            [InputActions.Skill4] = new(Keys.D4, Keys.NumPad4),
        };

        return config;
    }
}

internal readonly record struct InputBinding(Keys Primary, Keys? Alternate);

/// <summary>
/// Well-known action identifiers to avoid stringly-typed bindings.
/// </summary>
internal static class InputActions
{
    public const string MoveUp = "move.up";
    public const string MoveDown = "move.down";
    public const string MoveLeft = "move.left";
    public const string MoveRight = "move.right";

    public const string Pause = "pause";

    public const string MenuUp = "menu.up";
    public const string MenuDown = "menu.down";
    public const string MenuLeft = "menu.left";
    public const string MenuRight = "menu.right";
    public const string MenuConfirm = "menu.confirm";
    public const string MenuBack = "menu.back";

    public const string Attack = "attack";
    public const string Dash = "dash";
    public const string Interact = "interact";
    public const string PerkTree = "perk.tree";
    public const string Inventory = "inventory";
    public const string Respec = "respec";
    public const string Restart = "restart";

    public const string Skill1 = "skill.1";
    public const string Skill2 = "skill.2";
    public const string Skill3 = "skill.3";
    public const string Skill4 = "skill.4";
}

