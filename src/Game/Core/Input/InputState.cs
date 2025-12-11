using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Config;

namespace TheLastMageStanding.Game.Core.Input;

internal sealed class InputState
{
    private readonly SceneStateService? _sceneStateService;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private InputBindingsConfig _bindings;

    public Vector2 Movement { get; private set; }
    public Vector2 MouseScreenPosition { get; private set; }
    public Vector2 RawMouseScreenPosition { get; private set; }
    public bool PausePressed { get; private set; }
    public bool MenuUpPressed { get; private set; }
    public bool MenuDownPressed { get; private set; }
    public bool MenuConfirmPressed { get; private set; }
    public bool MenuLeftPressed { get; private set; }
    public bool MenuRightPressed { get; private set; }
    public bool MenuBackPressed { get; private set; }
    public bool RestartPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DebugTogglePressed { get; private set; }
    public bool PerkTreePressed { get; private set; }
    public bool RespecPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public string? LockedFeatureMessage { get; private set; }
    public bool CastSkill1Pressed { get; private set; }
    public bool CastSkill2Pressed { get; private set; }
    public bool CastSkill3Pressed { get; private set; }
    public bool CastSkill4Pressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool PrimaryClickPressed { get; private set; }

    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public InputState(
        SceneStateService? sceneStateService = null,
        int virtualWidth = 960,
        int virtualHeight = 540,
        InputBindingsConfig? bindings = null)
    {
        _sceneStateService = sceneStateService;
        _virtualWidth = Math.Max(1, virtualWidth);
        _virtualHeight = Math.Max(1, virtualHeight);
        _bindings = (bindings ?? InputBindingsConfig.Default).Clone();
        _bindings.Normalize();
    }

    public void ApplyBindings(InputBindingsConfig bindings)
    {
        _bindings = bindings.Clone();
        _bindings.Normalize();
    }

    public void Update(int viewportWidth, int viewportHeight)
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();

        // Capture raw mouse position in backbuffer pixels and scale into the virtual render resolution
        RawMouseScreenPosition = new Vector2(_currentMouse.X, _currentMouse.Y);

        var clampedViewportWidth = Math.Max(1, viewportWidth);
        var clampedViewportHeight = Math.Max(1, viewportHeight);

        // Backbuffer is already in pixel units (accounts for HiDPI); scale directly to virtual resolution.
        var scaleX = _virtualWidth / (float)clampedViewportWidth;
        var scaleY = _virtualHeight / (float)clampedViewportHeight;
        MouseScreenPosition = new Vector2(
            RawMouseScreenPosition.X * scaleX,
            RawMouseScreenPosition.Y * scaleY);

        var movement = Vector2.Zero;

        if (IsActionDown(InputActions.MoveUp))
        {
            movement.Y -= 1f;
        }

        if (IsActionDown(InputActions.MoveDown))
        {
            movement.Y += 1f;
        }

        if (IsActionDown(InputActions.MoveLeft))
        {
            movement.X -= 1f;
        }

        if (IsActionDown(InputActions.MoveRight))
        {
            movement.X += 1f;
        }

        Movement = movement == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(movement);
        PausePressed = IsNewActionPress(InputActions.Pause);
        MenuUpPressed = IsNewActionPress(InputActions.MenuUp);
        MenuDownPressed = IsNewActionPress(InputActions.MenuDown);
        MenuConfirmPressed = IsNewActionPress(InputActions.MenuConfirm);
        MenuLeftPressed = IsNewActionPress(InputActions.MenuLeft);
        MenuRightPressed = IsNewActionPress(InputActions.MenuRight);
        MenuBackPressed = IsNewActionPress(InputActions.MenuBack);
        RestartPressed = IsNewActionPress(InputActions.Restart);
        AttackPressed = IsActionDown(InputActions.Attack) || _currentMouse.LeftButton == ButtonState.Pressed;
        DashPressed = IsNewActionPress(InputActions.Dash);
        DebugTogglePressed = IsNewKeyPress(Keys.F3);
        InteractPressed = IsNewActionPress(InputActions.Interact);
        PrimaryClickPressed = _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

        // P and I keys toggle UI (work in both hub and stage, though changes only possible in hub)
        PerkTreePressed = IsNewActionPress(InputActions.PerkTree);
        InventoryPressed = IsNewActionPress(InputActions.Inventory);

        // Respec (Shift+R) - gate to hub only
        LockedFeatureMessage = null;
        var isInHub = _sceneStateService?.IsInHub() ?? false;
        var respecBinding = _bindings.GetBinding(InputActions.Respec);
        var respecNew = IsKeyNew(respecBinding.Primary) || (respecBinding.Alternate.HasValue && IsKeyNew(respecBinding.Alternate.Value));
        var shiftHeld = _currentKeyboard.IsKeyDown(Keys.LeftShift) || _currentKeyboard.IsKeyDown(Keys.RightShift);
        var shiftRespecPressed = respecNew && shiftHeld;
        if (shiftRespecPressed)
        {
            if (isInHub)
            {
                RespecPressed = true;
            }
            else
            {
                RespecPressed = false;
                LockedFeatureMessage = "Respec available in Hub";
            }
        }
        else
        {
            RespecPressed = false;
        }

        // Skill hotkeys (1-4) - raw input, filtered by game state in systems
        CastSkill1Pressed = IsActionDown(InputActions.Skill1);
        CastSkill2Pressed = IsActionDown(InputActions.Skill2);
        CastSkill3Pressed = IsActionDown(InputActions.Skill3);
        CastSkill4Pressed = IsActionDown(InputActions.Skill4);
    }

    private bool IsNewKeyPress(Keys key) =>
        _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private bool IsKeyNew(Keys key) => IsNewKeyPress(key);

    private bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);

    private bool IsActionDown(string actionId)
    {
        var binding = _bindings.GetBinding(actionId);
        if (binding.Primary != Keys.None && IsKeyDown(binding.Primary))
        {
            return true;
        }

        if (binding.Alternate.HasValue && binding.Alternate.Value != Keys.None && IsKeyDown(binding.Alternate.Value))
        {
            return true;
        }

        return false;
    }

    private bool IsNewActionPress(string actionId)
    {
        var binding = _bindings.GetBinding(actionId);
        if (binding.Primary != Keys.None && IsKeyNew(binding.Primary))
        {
            return true;
        }

        if (binding.Alternate.HasValue && binding.Alternate.Value != Keys.None && IsKeyNew(binding.Alternate.Value))
        {
            return true;
        }

        return false;
    }

    internal void SetTestState(Vector2 movement, bool attackPressed = false, bool dashPressed = false)
    {
        Movement = movement == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(movement);
        AttackPressed = attackPressed;
        DashPressed = dashPressed;
    }
}

