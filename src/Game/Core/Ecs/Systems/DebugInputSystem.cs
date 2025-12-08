using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles debug input commands like collision visualization toggle.
/// </summary>
internal sealed class DebugInputSystem : IUpdateSystem
{
    private readonly CollisionDebugRenderSystem _collisionDebugRender;
    private bool _previousF4State;
    private bool _previousF5State;
    private bool _previousF6State;

    public DebugInputSystem(CollisionDebugRenderSystem collisionDebugRender)
    {
        _collisionDebugRender = collisionDebugRender;
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
    }
}

