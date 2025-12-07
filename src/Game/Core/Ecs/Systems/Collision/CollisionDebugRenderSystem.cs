using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

/// <summary>
/// Debug rendering system for visualizing colliders in the world.
/// Toggle with a key or via config.
/// </summary>
internal sealed class CollisionDebugRenderSystem : IDrawSystem, IDisposable
{
    private Texture2D? _pixelTexture;
    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_enabled)
            return;

        // Create pixel texture if needed
        if (_pixelTexture == null)
        {
            _pixelTexture = new Texture2D(context.SpriteBatch.GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        var spriteBatch = context.SpriteBatch;
        var staticPool = world.GetPool<StaticCollider>();

        // Draw all colliders
        world.ForEach<Position, Collider>((Entity entity, ref Position pos, ref Collider col) =>
        {
            // Color code: Static = Cyan, Trigger = Yellow, Dynamic Solid = Lime
            var isStatic = staticPool.TryGet(entity, out _);
            var color = isStatic ? Color.Cyan * 0.4f : 
                        col.IsTrigger ? Color.Yellow * 0.3f : 
                        Color.Lime * 0.3f;
            var worldCenter = col.GetWorldCenter(pos.Value);

            if (col.Shape == ColliderShape.Circle)
            {
                DrawCircle(spriteBatch, worldCenter, col.Width, color, 24);
            }
            else if (col.Shape == ColliderShape.AABB)
            {
                var bounds = col.GetWorldBounds(pos.Value);
                DrawRectangle(spriteBatch, bounds, color);
            }

            // Draw center point (red for static, otherwise original color)
            DrawPoint(spriteBatch, worldCenter, isStatic ? Color.Blue : Color.Red);
        });
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 24)
    {
        if (_pixelTexture == null)
            return;

        float angleStep = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            var p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            var p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            DrawLine(spriteBatch, p1, p2, color, 2f);
        }
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (_pixelTexture == null)
            return;

        // Top
        DrawLine(spriteBatch,
            new Vector2(bounds.Left, bounds.Top),
            new Vector2(bounds.Right, bounds.Top),
            color, 2f);

        // Right
        DrawLine(spriteBatch,
            new Vector2(bounds.Right, bounds.Top),
            new Vector2(bounds.Right, bounds.Bottom),
            color, 2f);

        // Bottom
        DrawLine(spriteBatch,
            new Vector2(bounds.Right, bounds.Bottom),
            new Vector2(bounds.Left, bounds.Bottom),
            color, 2f);

        // Left
        DrawLine(spriteBatch,
            new Vector2(bounds.Left, bounds.Bottom),
            new Vector2(bounds.Left, bounds.Top),
            color, 2f);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixelTexture == null)
            return;

        var delta = end - start;
        var length = delta.Length();
        var angle = MathF.Atan2(delta.Y, delta.X);

        spriteBatch.Draw(
            _pixelTexture,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f
        );
    }

    private void DrawPoint(SpriteBatch spriteBatch, Vector2 position, Color color)
    {
        if (_pixelTexture == null)
            return;

        spriteBatch.Draw(
            _pixelTexture,
            position,
            null,
            color,
            0f,
            new Vector2(0.5f, 0.5f),
            new Vector2(4f, 4f),
            SpriteEffects.None,
            0f
        );
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}
