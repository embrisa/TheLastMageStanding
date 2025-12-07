using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Animates and renders floating damage numbers in world space.
/// </summary>
internal sealed class DamageNumberSystem : IUpdateSystem, IDrawSystem, ILoadContentSystem
{
    private SpriteFont? _font;
    private bool _contentLoaded;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _contentLoaded = true;
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_contentLoaded)
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<DamageNumber, Position, Lifetime>(
            (Entity entity, ref DamageNumber number, ref Position position, ref Lifetime lifetime) =>
            {
                var floatDistance = number.FloatSpeed * deltaSeconds;
                position.Value += new Vector2(number.HorizontalJitter * deltaSeconds, -floatDistance);
                world.SetComponent(entity, position);
            });
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_contentLoaded || _font is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;

        world.ForEach<DamageNumber, Position, Lifetime>(
            (Entity entity, ref DamageNumber number, ref Position position, ref Lifetime lifetime) =>
            {
                if (number.LifetimeSeconds <= 0f)
                {
                    return;
                }

                var progress = MathHelper.Clamp(
                    1f - (lifetime.RemainingSeconds / number.LifetimeSeconds),
                    0f,
                    1f);

                var alpha = progress < 0.2f
                    ? MathHelper.Lerp(0f, 1f, progress / 0.2f)
                    : (progress > 0.65f ? MathHelper.Lerp(1f, 0f, (progress - 0.65f) / 0.35f) : 1f);

                var text = MathF.Round(number.Amount).ToString("0", CultureInfo.InvariantCulture);
                var scale = number.Scale;
                var size = _font.MeasureString(text) * scale;
                var origin = size * 0.5f;
                var color = number.Color * alpha;

                spriteBatch.DrawString(
                    _font,
                    text,
                    position.Value,
                    color,
                    0f,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f);
            });
    }
}

