using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles debug input commands like collision visualization toggle.
/// </summary>
internal sealed class DebugInputSystem : IUpdateSystem
{
    private readonly CollisionDebugRenderSystem _collisionDebugRender;
    private readonly EnemyEntityFactory _enemyFactory;
    private readonly StatusEffectDebugSystem _statusDebugSystem;
    private readonly AiDebugRenderSystem _aiDebugRenderSystem;
    private bool _previousD1State;
    private bool _previousD2State;
    private bool _previousD3State;
    private bool _previousD4State;
    private bool _previousF4State;
    private bool _previousF5State;
    private bool _previousF6State;
    private bool _previousF7State;
    private bool _previousF8State;
    private bool _previousF9State;
    private bool _previousF10State;
    private bool _previousF11State;

    public DebugInputSystem(
        CollisionDebugRenderSystem collisionDebugRender,
        EnemyEntityFactory enemyFactory,
        StatusEffectDebugSystem statusDebugSystem,
        AiDebugRenderSystem aiDebugRenderSystem)
    {
        _collisionDebugRender = collisionDebugRender;
        _enemyFactory = enemyFactory;
        _statusDebugSystem = statusDebugSystem;
        _aiDebugRenderSystem = aiDebugRenderSystem;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (context.Input.DebugTogglePressed)
        {
            _collisionDebugRender.Enabled = !_collisionDebugRender.Enabled;
            System.Console.WriteLine($"[Debug] Collision visualization: {(_collisionDebugRender.Enabled ? "ON" : "OFF")}");
        }

        var keyboardState = Keyboard.GetState();
        var shiftHeld = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        HandleModifierHotkey(world, keyboardState, shiftHeld, Keys.D1, ref _previousD1State, EliteModifierType.ExtraProjectiles);
        HandleModifierHotkey(world, keyboardState, shiftHeld, Keys.D2, ref _previousD2State, EliteModifierType.Vampiric);
        HandleModifierHotkey(world, keyboardState, shiftHeld, Keys.D3, ref _previousD3State, EliteModifierType.ExplosiveDeath);
        HandleModifierHotkey(world, keyboardState, shiftHeld, Keys.D4, ref _previousD4State, EliteModifierType.Shielded);

        // F4: Toggle hit-stop
        var f4Pressed = keyboardState.IsKeyDown(Keys.F4);
        if (f4Pressed && !_previousF4State)
        {
            HitStopSystem.EnableHitStop = !HitStopSystem.EnableHitStop;
            System.Console.WriteLine($"[Debug] Hit-stop: {(HitStopSystem.EnableHitStop ? "ON" : "OFF")}");
        }
        _previousF4State = f4Pressed;

        // F5: Toggle camera shake
        var f5Pressed = keyboardState.IsKeyDown(Keys.F5);
        if (f5Pressed && !_previousF5State)
        {
            HitStopSystem.EnableCameraShake = !HitStopSystem.EnableCameraShake;
            System.Console.WriteLine($"[Debug] Camera shake: {(HitStopSystem.EnableCameraShake ? "ON" : "OFF")}");
        }
        _previousF5State = f5Pressed;

        // F6: Toggle VFX/SFX
        var f6Pressed = keyboardState.IsKeyDown(Keys.F6);
        if (f6Pressed && !_previousF6State)
        {
            VfxSystem.EnableVfx = !VfxSystem.EnableVfx;
            System.Console.WriteLine($"[Debug] VFX: {(VfxSystem.EnableVfx ? "ON" : "OFF")}");
        }
        _previousF6State = f6Pressed;

        // F7: Spawn elite enemy at player position
        var f7Pressed = keyboardState.IsKeyDown(Keys.F7);
        if (f7Pressed && !_previousF7State)
        {
            SpawnDebugElite(world);
        }
        _previousF7State = f7Pressed;

        // F8: Spawn boss enemy at player position
        var f8Pressed = keyboardState.IsKeyDown(Keys.F8);
        if (f8Pressed && !_previousF8State)
        {
            SpawnDebugBoss(world);
        }
        _previousF8State = f8Pressed;

        // F9: Toggle dash/i-frame debug overlay
        var f9Pressed = keyboardState.IsKeyDown(Keys.F9);
        if (f9Pressed && !_previousF9State)
        {
            _collisionDebugRender.ShowDashDebug = !_collisionDebugRender.ShowDashDebug;
            System.Console.WriteLine($"[Debug] Dash debug: {(_collisionDebugRender.ShowDashDebug ? "ON" : "OFF")}");
        }
        _previousF9State = f9Pressed;

        // F10: Toggle status effect overlay
        var f10Pressed = keyboardState.IsKeyDown(Keys.F10);
        if (f10Pressed && !_previousF10State)
        {
            _statusDebugSystem.Enabled = !_statusDebugSystem.Enabled;
            System.Console.WriteLine($"[Debug] Status overlay: {(_statusDebugSystem.Enabled ? "ON" : "OFF")}");
        }
        _previousF10State = f10Pressed;

        // F11: Toggle AI state overlay
        var f11Pressed = keyboardState.IsKeyDown(Keys.F11);
        if (f11Pressed && !_previousF11State)
        {
            _aiDebugRenderSystem.Enabled = !_aiDebugRenderSystem.Enabled;
            System.Console.WriteLine($"[Debug] AI overlay: {(_aiDebugRenderSystem.Enabled ? "ON" : "OFF")}");
        }
        _previousF11State = f11Pressed;
    }

    private void SpawnDebugElite(EcsWorld world)
    {
        if (!TryGetPlayerPosition(world, out var playerPosition))
        {
            return;
        }

        // Spawn elite slightly offset from player
        var spawnPosition = playerPosition + new Vector2(80f, 0f);
        var eliteArchetype = EnemyWaveConfig.CreateEliteForDebug();
        _enemyFactory.CreateEnemy(spawnPosition, eliteArchetype);
        System.Console.WriteLine($"[Debug] Spawned elite enemy at {spawnPosition}");
    }

    private void SpawnDebugBoss(EcsWorld world)
    {
        if (!TryGetPlayerPosition(world, out var playerPosition))
        {
            return;
        }

        // Spawn boss slightly offset from player
        var spawnPosition = playerPosition + new Vector2(120f, 0f);
        var bossArchetype = EnemyWaveConfig.CreateBossForDebug();
        _enemyFactory.CreateEnemy(spawnPosition, bossArchetype);
        System.Console.WriteLine($"[Debug] Spawned boss enemy at {spawnPosition}");
    }

    private void HandleModifierHotkey(
        EcsWorld world,
        KeyboardState keyboardState,
        bool shiftHeld,
        Keys key,
        ref bool previousState,
        params EliteModifierType[] modifiers)
    {
        var pressed = shiftHeld && keyboardState.IsKeyDown(key);
        if (pressed && !previousState)
        {
            SpawnEliteWithModifiers(world, modifiers);
        }

        previousState = pressed;
    }

    private void SpawnEliteWithModifiers(EcsWorld world, params EliteModifierType[] modifiers)
    {
        if (!TryGetPlayerPosition(world, out var playerPosition))
        {
            return;
        }

        var spawnPosition = playerPosition + new Vector2(100f, 40f);
        var eliteArchetype = EnemyWaveConfig.CreateEliteForDebug();
        _enemyFactory.CreateEnemy(spawnPosition, eliteArchetype, modifiers);
        System.Console.WriteLine($"[Debug] Spawned elite with modifiers: {string.Join(",", modifiers)} at {spawnPosition}");
    }

    private static bool TryGetPlayerPosition(EcsWorld world, out Vector2 position)
    {
        var found = false;
        var captured = Vector2.Zero;

        world.ForEach<PlayerTag, Position>(
            (Entity _, ref PlayerTag _, ref Position pos) =>
            {
                captured = pos.Value;
                found = true;
            });

        position = captured;
        return found;
    }
}
