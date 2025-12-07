using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class RenderDebugSystem : IDrawSystem, ILoadContentSystem
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
        world.ForEach<Position, RenderDebug>(
            (Entity entity, ref Position position, ref RenderDebug render) =>
            {
                var origin = new Vector2(0.5f, 0.5f);
                spriteBatch.Draw(
                    _pixel,
                    position.Value,
                    null,
                    render.Fill,
                    0f,
                    origin,
                    render.Size,
                    SpriteEffects.None,
                    0f);

                if (!render.ShowHealthBar || !world.TryGetComponent(entity, out Health health))
                {
                    return;
                }

                DrawHealthBar(spriteBatch, position.Value, health);
            });
    }

    private void DrawHealthBar(SpriteBatch spriteBatch, Vector2 position, Health health)
    {
        const float barWidth = 26f;
        const float barHeight = 3f;
        var background = Color.DarkGray;
        var fillColor = Color.Lerp(Color.Red, Color.LimeGreen, MathHelper.Clamp(health.Ratio, 0f, 1f));
        var barPosition = position + new Vector2(-barWidth * 0.5f, -12f);

        spriteBatch.Draw(_pixel, barPosition, null, background, 0f, Vector2.Zero, new Vector2(barWidth, barHeight), SpriteEffects.None, 0f);
        spriteBatch.Draw(
            _pixel,
            barPosition,
            null,
            fillColor,
            0f,
            Vector2.Zero,
            new Vector2(barWidth * MathHelper.Clamp(health.Ratio, 0f, 1f), barHeight),
            SpriteEffects.None,
            0f);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}

