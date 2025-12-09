using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Input;

internal sealed class InputState
{
    private readonly SceneStateService? _sceneStateService;

    public Vector2 Movement { get; private set; }
    public Vector2 MouseScreenPosition { get; private set; }
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

    public InputState(SceneStateService? sceneStateService = null)
    {
        _sceneStateService = sceneStateService;
    }

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
        
        // Capture mouse screen position
        MouseScreenPosition = new Vector2(_currentMouse.X, _currentMouse.Y);
        
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
        InteractPressed = IsNewKeyPress(Keys.E);
        PrimaryClickPressed = _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

        // P and I keys toggle UI (work in both hub and stage, though changes only possible in hub)
        PerkTreePressed = IsNewKeyPress(Keys.P);
        InventoryPressed = IsNewKeyPress(Keys.I);

        // Respec (Shift+R) - gate to hub only
        LockedFeatureMessage = null;
        var isInHub = _sceneStateService?.IsInHub() ?? false;
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

        // Skill hotkeys (1-4) - raw input, filtered by game state in systems
        CastSkill1Pressed = _currentKeyboard.IsKeyDown(Keys.D1) || _currentKeyboard.IsKeyDown(Keys.NumPad1);
        CastSkill2Pressed = _currentKeyboard.IsKeyDown(Keys.D2) || _currentKeyboard.IsKeyDown(Keys.NumPad2);
        CastSkill3Pressed = _currentKeyboard.IsKeyDown(Keys.D3) || _currentKeyboard.IsKeyDown(Keys.NumPad3);
        CastSkill4Pressed = _currentKeyboard.IsKeyDown(Keys.D4) || _currentKeyboard.IsKeyDown(Keys.NumPad4);
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

