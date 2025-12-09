using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders NPCs as colored rectangles (placeholder visuals).
/// </summary>
internal sealed class NpcRenderSystem : IDrawSystem, ILoadContentSystem, IDisposable
{
    private Texture2D? _whitePixel;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (_whitePixel == null)
            return;

        var spriteBatch = context.SpriteBatch;

        // Draw all NPC entities
        world.ForEach<InteractionTrigger, Position>(
            (Entity _, ref InteractionTrigger trigger, ref Position position) =>
            {
                // Draw NPC as colored square
                var color = GetNpcColor(trigger.Type);
                var size = 32;
                var rect = new Rectangle(
                    (int)(position.Value.X - size / 2),
                    (int)(position.Value.Y - size / 2),
                    size,
                    size
                );
                spriteBatch.Draw(_whitePixel, rect, color);
                
                // Draw interaction radius (debug)
                // Circle approximation with rectangles
                var radiusColor = new Color(color, 0.2f);
                for (int angle = 0; angle < 360; angle += 10)
                {
                    var rad = MathHelper.ToRadians(angle);
                    var x = position.Value.X + (float)Math.Cos(rad) * trigger.InteractionRadius;
                    var y = position.Value.Y + (float)Math.Sin(rad) * trigger.InteractionRadius;
                    var dotRect = new Rectangle((int)x - 1, (int)y - 1, 2, 2);
                    spriteBatch.Draw(_whitePixel, dotRect, radiusColor);
                }
            });
    }

    private static Color GetNpcColor(InteractionType type)
    {
        return type switch
        {
            InteractionType.OpenTalentTree => Color.Purple,
            InteractionType.OpenStageSelection => Color.Red,
            InteractionType.OpenSkillSelection => Color.Blue,
            InteractionType.OpenShop => Color.Gold,
            InteractionType.OpenStats => Color.Green,
            _ => Color.White
        };
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
