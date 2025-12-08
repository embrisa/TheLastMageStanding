using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Input;

internal sealed class InputState
{
    private readonly SceneStateService? _sceneStateService;

    public Vector2 Movement { get; private set; }
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

    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _currentMouse;

    public InputState(SceneStateService? sceneStateService = null)
    {
        _sceneStateService = sceneStateService;
    }

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;

        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
        var movement = Vector2.Zero;

        if (_currentKeyboard.IsKeyDown(Keys.W) || _currentKeyboard.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1f;
        }

        if (_currentKeyboard.IsKeyDown(Keys.S) || _currentKeyboard.IsKeyDown(Keys.Down))
        {
            movement.Y += 1f;
        }

        if (_currentKeyboard.IsKeyDown(Keys.A) || _currentKeyboard.IsKeyDown(Keys.Left))
        {
            movement.X -= 1f;
        }

        if (_currentKeyboard.IsKeyDown(Keys.D) || _currentKeyboard.IsKeyDown(Keys.Right))
        {
            movement.X += 1f;
        }

        Movement = movement == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(movement);
        PausePressed = IsNewKeyPress(Keys.Escape);
        MenuUpPressed = IsNewKeyPress(Keys.Up) || IsNewKeyPress(Keys.W);
        MenuDownPressed = IsNewKeyPress(Keys.Down) || IsNewKeyPress(Keys.S);
        MenuConfirmPressed = IsNewKeyPress(Keys.Enter) || IsNewKeyPress(Keys.Space);
        MenuLeftPressed = IsNewKeyPress(Keys.Left) || IsNewKeyPress(Keys.A);
        MenuRightPressed = IsNewKeyPress(Keys.Right) || IsNewKeyPress(Keys.D);
        MenuBackPressed = IsNewKeyPress(Keys.Escape) || IsNewKeyPress(Keys.Back);
        RestartPressed = IsNewKeyPress(Keys.R);
        AttackPressed = _currentKeyboard.IsKeyDown(Keys.J) || _currentMouse.LeftButton == ButtonState.Pressed;
        DashPressed = IsNewKeyPress(Keys.LeftShift) || IsNewKeyPress(Keys.RightShift) || IsNewKeyPress(Keys.Space);
        DebugTogglePressed = IsNewKeyPress(Keys.F3);

        // Gate perk tree, inventory, and respec based on scene
        LockedFeatureMessage = null;
        var isInHub = _sceneStateService?.IsInHub() ?? false;

        if (IsNewKeyPress(Keys.P))
        {
            if (isInHub)
            {
                PerkTreePressed = true;
            }
            else
            {
                PerkTreePressed = false;
                LockedFeatureMessage = "Perk Tree available in Hub";
            }
        }
        else
        {
            PerkTreePressed = false;
        }

        if (IsNewKeyPress(Keys.I))
        {
            if (isInHub)
            {
                InventoryPressed = true;
            }
            else
            {
                InventoryPressed = false;
                LockedFeatureMessage = "Inventory available in Hub";
            }
        }
        else
        {
            InventoryPressed = false;
        }

        var shiftRPressed = IsNewKeyPress(Keys.R) && (_currentKeyboard.IsKeyDown(Keys.LeftShift) || _currentKeyboard.IsKeyDown(Keys.RightShift));
        if (shiftRPressed)
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
    }

    private bool IsNewKeyPress(Keys key) =>
        _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    internal void SetTestState(Vector2 movement, bool attackPressed = false, bool dashPressed = false)
    {
        Movement = movement == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(movement);
        AttackPressed = attackPressed;
        DashPressed = dashPressed;
    }
}

