using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Input;

namespace TheLastMageStanding.Game.Core.Player;

internal sealed class PlayerCharacter
{
    private readonly Camera2D _camera;
    private Texture2D? _debugDot;
    private float _attackTimer;
    private bool _attackTriggered;

    public Vector2 Position { get; private set; } = Vector2.Zero;
    public float MoveSpeed { get; set; } = 220f;
    public float MaxHealth { get; private set; } = 100f;
    public float Health { get; private set; } = 100f;
    public float AttackDamage { get; set; } = 20f;
    public float AttackRange { get; set; } = 42f;
    public float AttackCooldown { get; set; } = 0.35f;
    public float CollisionRadius { get; set; } = 6f;
    public bool IsDead => Health <= 0f;
    public float HealthRatio => MaxHealth <= 0f ? 0f : Health / MaxHealth;

    public PlayerCharacter(Camera2D camera)
    {
        _camera = camera;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // 1x1 white pixel for debug rendering and placeholder visuals.
        _debugDot ??= CreatePixel(graphicsDevice);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _attackTimer = MathF.Max(0f, _attackTimer - delta);

        Position += input.Movement * MoveSpeed * delta;
        if (!IsDead && input.AttackPressed && _attackTimer <= 0f)
        {
            _attackTriggered = true;
            _attackTimer = AttackCooldown;
        }

        _camera.LookAt(Position);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_debugDot is null)
        {
            return;
        }

        const float size = 8f;
        var origin = new Vector2(0.5f, 0.5f);
        var bodyColor = Color.Lerp(Color.Red, Color.LimeGreen, MathHelper.Clamp(HealthRatio, 0f, 1f));
        spriteBatch.Draw(_debugDot, Position, null, bodyColor, 0f, origin, size, SpriteEffects.None, 0f);

        const float barWidth = 30f;
        const float barHeight = 3f;
        var barPosition = Position + new Vector2(-barWidth * 0.5f, -12f);
        spriteBatch.Draw(_debugDot, barPosition, null, Color.DimGray, 0f, Vector2.Zero, new Vector2(barWidth, barHeight), SpriteEffects.None, 0f);
        spriteBatch.Draw(
            _debugDot,
            barPosition,
            null,
            bodyColor,
            0f,
            Vector2.Zero,
            new Vector2(barWidth * MathHelper.Clamp(HealthRatio, 0f, 1f), barHeight),
            SpriteEffects.None,
            0f);
    }

    public bool ConsumeAttackTrigger()
    {
        var triggered = _attackTriggered;
        _attackTriggered = false;
        return triggered;
    }

    public void ApplyDamage(float amount)
    {
        Health = MathF.Max(0f, Health - amount);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}

