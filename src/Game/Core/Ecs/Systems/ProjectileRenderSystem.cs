using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders projectiles as colored circles in world space.
/// </summary>
internal sealed class ProjectileRenderSystem : IDrawSystem, ILoadContentSystem
{
    private Texture2D? _pixel;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _pixel ??= CreatePixel(graphicsDevice);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (_pixel is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;

        // Draw ranged enemy windup telegraphs
        world.ForEach<RangedAttacker, Position>(
            (Entity _, ref RangedAttacker ranged, ref Position position) =>
            {
                if (!ranged.IsWindingUp)
                {
                    return;
                }

                // Draw a pulsing circle to indicate windup
                var windupProgress = ranged.WindupTimer / ranged.WindupSeconds;
                var alpha = (byte)(128 + (127 * MathF.Sin(windupProgress * MathF.PI * 4f)));
                var telegraphColor = new Color((byte)255, (byte)200, (byte)100, alpha);
                var radius = 8f + (windupProgress * 6f);

                spriteBatch.Draw(
                    _pixel,
                    position.Value,
                    null,
                    telegraphColor,
                    0f,
                    new Vector2(0.5f, 0.5f),
                    radius * 2f,
                    SpriteEffects.None,
                    0f);
            });

        // Draw projectiles
        world.ForEach<Projectile, Position, ProjectileVisual>(
            (Entity _, ref Projectile _, ref Position position, ref ProjectileVisual visual) =>
            {
                // Draw projectile as a circle
                var diameter = visual.Radius * 2f;
                spriteBatch.Draw(
                    _pixel,
                    position.Value,
                    null,
                    visual.Color,
                    0f,
                    new Vector2(0.5f, 0.5f),
                    diameter,
                    SpriteEffects.None,
                    0f);
            });
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}
