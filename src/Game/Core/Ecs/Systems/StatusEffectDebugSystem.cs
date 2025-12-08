using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Optional overlay that renders active status effects above entities for debugging.
/// </summary>
internal sealed class StatusEffectDebugSystem : IDrawSystem, ILoadContentSystem
{
    private SpriteFont? _font;
    public bool Enabled { get; set; }

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!Enabled || _font is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;

        world.ForEach<ActiveStatusEffects, Position>(
            (Entity entity, ref ActiveStatusEffects active, ref Position position) =>
            {
                if (active.Effects == null || active.Effects.Count == 0)
                {
                    return;
                }

                var offset = new Vector2(0f, -24f);
                var lineHeight = 14f;
                var lineIndex = 0;

                foreach (var effect in active.Effects)
                {
                    var text = $"{effect.Data.Type} ({effect.RemainingDuration:F1}s) x{effect.CurrentStacks}";
                    var size = _font.MeasureString(text);
                    var origin = size * 0.5f;
                    var color = GetColor(effect.Data.Type);

                    spriteBatch.DrawString(
                        _font,
                        text,
                        position.Value + offset + new Vector2(0, lineIndex * lineHeight),
                        color,
                        0f,
                        origin,
                        0.6f,
                        SpriteEffects.None,
                        0f);

                    lineIndex++;
                }
            });
    }

    private static Color GetColor(StatusEffectType type) =>
        type switch
        {
            StatusEffectType.Burn => new Color(255, 120, 50),
            StatusEffectType.Freeze => new Color(100, 200, 255),
            StatusEffectType.Slow => new Color(150, 180, 255),
            StatusEffectType.Shock => new Color(150, 100, 255),
            StatusEffectType.Poison => new Color(50, 200, 80),
            _ => Color.White
        };
}

