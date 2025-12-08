using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Debug;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Optional overlay to display AI role/state info and debug ranges.
/// </summary>
internal sealed class AiDebugRenderSystem : IDrawSystem, ILoadContentSystem, IDisposable
{
    private SpriteFont? _font;
    private Texture2D? _pixel;

    public bool Enabled { get; set; }

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!Enabled || _font is null || _pixel is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;

        world.ForEach<Position, AiRoleConfig, AiBehaviorStateMachine>(
            (Entity entity, ref Position position, ref AiRoleConfig role, ref AiBehaviorStateMachine state) =>
            {
                var text = AiRoleInspector.InspectAiBehavior(world, entity);
                var color = StateColor(state.State);

                // Position text slightly above the entity, use visual origin if present
                var textPos = position.Value + new Vector2(0f, -28f);
                if (world.TryGetComponent(entity, out EnemyVisual visual))
                {
                    textPos.Y = position.Value.Y - visual.Origin.Y * visual.Scale - 8f;
                }

                var size = _font.MeasureString(text);
                spriteBatch.DrawString(
                    _font,
                    text,
                    textPos,
                    color,
                    0f,
                    size * 0.5f,
                    0.6f,
                    SpriteEffects.None,
                    0f);

                DrawRanges(spriteBatch, position.Value, role, color);
            });
    }

    private void DrawRanges(SpriteBatch spriteBatch, Vector2 center, AiRoleConfig role, Color color)
    {
        if (_pixel is null)
        {
            return;
        }

        switch (role.Role)
        {
            case EnemyRole.Charger:
                DrawCircle(spriteBatch, center, role.CommitRangeMin, Color.Yellow * 0.35f);
                DrawCircle(spriteBatch, center, role.CommitRangeMax, Color.Yellow * 0.6f);
                break;
            case EnemyRole.Protector:
                DrawCircle(spriteBatch, center, role.ShieldRange, new Color(80, 140, 255) * 0.45f);
                break;
            case EnemyRole.Buffer:
                DrawCircle(spriteBatch, center, role.BuffRange, new Color(120, 220, 120) * 0.45f);
                break;
        }
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 24)
    {
        if (radius <= 0f || _pixel is null)
        {
            return;
        }

        var angleStep = MathHelper.TwoPi / segments;
        for (var i = 0; i < segments; i++)
        {
            var angle1 = i * angleStep;
            var angle2 = (i + 1) * angleStep;

            var p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            var p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            DrawLine(spriteBatch, p1, p2, color, 2f);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixel is null)
        {
            return;
        }

        var delta = end - start;
        var length = delta.Length();
        var angle = MathF.Atan2(delta.Y, delta.X);

        spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    private static Color StateColor(AiBehaviorState state) =>
        state switch
        {
            AiBehaviorState.Seeking => Color.White,
            AiBehaviorState.Committing => Color.Yellow,
            AiBehaviorState.Buffing => Color.LimeGreen,
            AiBehaviorState.Shielding => new Color(80, 140, 255),
            AiBehaviorState.Cooldown => Color.Gray,
            AiBehaviorState.Idle => Color.LightGray,
            _ => Color.White
        };

    public void Dispose()
    {
        _font = null;
        _pixel?.Dispose();
        _pixel = null;
    }
}

