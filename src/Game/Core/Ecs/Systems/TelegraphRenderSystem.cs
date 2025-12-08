using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders telegraph warnings and VFX effects.
/// </summary>
internal sealed class TelegraphRenderSystem : IDrawSystem
{
    private Texture2D? _pixelTexture;

    public void Initialize(EcsWorld world)
    {
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!TelegraphSystem.ShowTelegraphs)
            return;

        _pixelTexture ??= CreatePixelTexture(context.SpriteBatch.GraphicsDevice);

        var spriteBatch = context.SpriteBatch;

        // Draw telegraphs
        world.ForEach<ActiveTelegraph, Position>((Entity _, ref ActiveTelegraph telegraph, ref Position position) =>
        {
            DrawTelegraph(spriteBatch, telegraph, position.Value);
        });

        // Draw VFX
        if (VfxSystem.EnableVfx)
        {
            world.ForEach<ActiveVfx, Position>((Entity _, ref ActiveVfx vfx, ref Position position) =>
            {
                DrawVfx(spriteBatch, vfx, position.Value);
            });
        }
    }

    private void DrawTelegraph(SpriteBatch spriteBatch, ActiveTelegraph telegraph, Vector2 position)
    {
        if (_pixelTexture == null) return;

        var data = telegraph.Data;

        switch (data.Shape)
        {
            case TelegraphShape.Circle:
                DrawCircleTelegraph(spriteBatch, position, data.Radius, data.Color);
                break;
            case TelegraphShape.Cone:
                // Future: Implement cone shape
                DrawCircleTelegraph(spriteBatch, position, data.Radius, data.Color);
                break;
            case TelegraphShape.Rectangle:
                // Future: Implement rectangle shape
                DrawCircleTelegraph(spriteBatch, position, data.Radius, data.Color);
                break;
        }
    }

    private void DrawCircleTelegraph(SpriteBatch spriteBatch, Vector2 position, float radius, Color color)
    {
        if (_pixelTexture == null) return;

        // Draw simple filled circle using pixel texture
        var diameter = radius * 2f;
        spriteBatch.Draw(
            _pixelTexture,
            position,
            null,
            color,
            0f,
            new Vector2(0.5f),
            diameter,
            SpriteEffects.None,
            0f
        );
    }

    private void DrawVfx(SpriteBatch spriteBatch, ActiveVfx vfx, Vector2 position)
    {
        if (_pixelTexture == null) return;

        // Simple flash effect for now (can be extended with sprites/particles later)
        var size = vfx.Type switch
        {
            VfxType.Impact => 12f,
            VfxType.WindupFlash => 8f,
            VfxType.ProjectileTrail => 6f,
            VfxType.MuzzleFlash => 10f,
            _ => 8f
        };

        spriteBatch.Draw(
            _pixelTexture,
            position,
            null,
            vfx.Color,
            0f,
            new Vector2(0.5f),
            size * vfx.Scale,
            SpriteEffects.None,
            0f
        );
    }

    private static Texture2D CreatePixelTexture(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}
