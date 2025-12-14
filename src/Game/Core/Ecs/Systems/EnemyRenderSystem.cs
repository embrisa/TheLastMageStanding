using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class EnemyRenderSystem : IUpdateSystem, IDrawSystem, ILoadContentSystem
{
    private Texture2D? _pixel;
    private ContentManager? _content;
    private bool _contentLoaded;
    private readonly Dictionary<string, Texture2D> _textureCache = new();
    private readonly Dictionary<EnemySpriteKey, EnemySpriteSet> _spriteCache = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _pixel ??= CreatePixel(graphicsDevice);
        _content = content;
        _contentLoaded = true;
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_contentLoaded)
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<EnemyAnimationState, EnemySpriteAssets, Velocity>(
            (Entity entity, ref EnemyAnimationState state, ref EnemySpriteAssets assets, ref Velocity velocity) =>
            {
                if (!world.TryGetComponent(entity, out EnemyVisual visual))
                {
                    return;
                }

                EnsureSpriteSet(world, entity, assets, visual.FrameSize);

                var isMoving = velocity.Value.LengthSquared() > 0.0001f;
                var facing = isMoving ? ToFacing(velocity.Value) : state.Facing;
                var clip = isMoving ? EnemyAnimationClip.Run : EnemyAnimationClip.Idle;

                if (clip != state.ActiveClip)
                {
                    state.ActiveClip = clip;
                    state.FrameIndex = 0;
                    state.Timer = 0f;
                }

                state.IsMoving = isMoving;
                state.Facing = facing;

                if (!world.TryGetComponent(entity, out EnemySpriteSet spriteSet))
                {
                    return;
                }

                var animation = GetAnimation(spriteSet, state.ActiveClip);
                var frameDuration = animation.FrameDurationSeconds <= 0f ? 0.1f : animation.FrameDurationSeconds;
                var frameCount = Math.Max(1, animation.Columns);

                state.Timer += deltaSeconds;
                while (state.Timer >= frameDuration)
                {
                    state.Timer -= frameDuration;
                    state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                }
            });
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_contentLoaded || _pixel is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;

        world.ForEach<Position, EnemyAnimationState, EnemySpriteSet>(
            (Entity entity, ref Position position, ref EnemyAnimationState state, ref EnemySpriteSet sprites) =>
            {
                if (!world.TryGetComponent(entity, out EnemyVisual visual))
                {
                    return;
                }

                var animation = GetAnimation(sprites, state.ActiveClip);
                var frameWidth = animation.FrameWidth;
                var frameHeight = animation.FrameHeight;
                var frameCount = Math.Max(1, animation.Columns);
                var frameIndex = Math.Clamp(state.FrameIndex, 0, frameCount - 1);
                var column = frameIndex % animation.Columns;
                var row = RowForFacing(state.Facing);
                row = Math.Clamp(row, 0, Math.Max(0, animation.Rows - 1));
                var source = new Rectangle(column * frameWidth, row * frameHeight, frameWidth, frameHeight);

                var tint = visual.Tint;
                if (world.TryGetComponent(entity, out HitFlash flash))
                {
                    var flashStrength = MathHelper.Clamp(flash.RemainingSeconds / 0.12f, 0f, 1f);
                    tint = Color.Lerp(tint, Color.OrangeRed, flashStrength * 0.8f);
                }

                if (world.TryGetComponent(entity, out StatusEffectVisual statusVisual) && statusVisual.Strength > 0f)
                {
                    var strength = MathHelper.Clamp(statusVisual.Strength, 0f, 1f);
                    tint = Color.Lerp(tint, statusVisual.Color, strength);
                }

                spriteBatch.Draw(
                    animation.Texture,
                    position.Value,
                    source,
                    tint,
                    0f,
                    visual.Origin,
                    visual.Scale,
                    SpriteEffects.None,
                    0f);

                // Draw elite/boss indicators
                DrawTierIndicator(world, entity, spriteBatch, position.Value, visual);

                DrawHealthBar(world, entity, spriteBatch, position.Value, visual);
            });
    }

    private void DrawTierIndicator(EcsWorld world, Entity entity, SpriteBatch spriteBatch, Vector2 position, EnemyVisual visual)
    {
        if (_pixel is null)
        {
            return;
        }

        var hasElite = world.TryGetComponent(entity, out EliteTag _);
        var hasBoss = world.TryGetComponent(entity, out BossTag _);

        if (!hasElite && !hasBoss)
        {
            return;
        }

        // Draw a ring around elite/boss enemies
        var color = hasElite ? new Color(255, 200, 50, 180) : new Color(150, 50, 200, 200); // Gold for elite, purple for boss
        var radius = visual.Scale * 20f;
        var thickness = hasElite ? 2f : 3f;
        var segments = 32;

        for (var i = 0; i < segments; i++)
        {
            var angle1 = (float)i / segments * MathF.Tau;
            var angle2 = (float)(i + 1) / segments * MathF.Tau;

            var p1 = position + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            var p2 = position + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            var delta = p2 - p1;
            var length = delta.Length();
            var angle = MathF.Atan2(delta.Y, delta.X);

            spriteBatch.Draw(_pixel, p1, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }

    private void EnsureSpriteSet(EcsWorld world, Entity entity, EnemySpriteAssets assets, int frameSize)
    {
        if (_content is null)
        {
            return;
        }

        var key = new EnemySpriteKey(assets.IdleAsset, assets.RunAsset, frameSize);
        if (!_spriteCache.TryGetValue(key, out var spriteSet))
        {
            var idleTexture = LoadTexture(assets.IdleAsset);
            var runTexture = LoadTexture(assets.RunAsset);
            spriteSet = new EnemySpriteSet
            {
                Idle = BuildAnimation(idleTexture, frameSize, frameSize, fps: 6f),
                Run = BuildAnimation(runTexture, frameSize, frameSize, fps: 12f),
            };

            _spriteCache[key] = spriteSet;
        }

        if (!world.TryGetComponent(entity, out EnemySpriteSet _))
        {
            world.SetComponent(entity, spriteSet);
        }
    }

    private Texture2D LoadTexture(string asset)
    {
        if (_textureCache.TryGetValue(asset, out var texture))
        {
            return texture;
        }

        if (_content is null)
        {
            throw new InvalidOperationException("Content manager not initialized for enemy rendering.");
        }

        texture = _content.Load<Texture2D>(asset);
        _textureCache[asset] = texture;
        return texture;
    }

    private void DrawHealthBar(EcsWorld world, Entity entity, SpriteBatch spriteBatch, Vector2 position, EnemyVisual visual)
    {
        if (!world.TryGetComponent(entity, out Health health))
        {
            return;
        }

        const float barWidth = 26f;
        const float barHeight = 3f;
        var ratio = MathHelper.Clamp(health.Ratio, 0f, 1f);
        var fillColor = Color.Lerp(Color.Red, Color.LimeGreen, ratio);

        var offsetY = -visual.Origin.Y * visual.Scale - 10f;
        var barPosition = position + new Vector2(-barWidth * 0.5f, offsetY);

        spriteBatch.Draw(_pixel, barPosition, null, Color.DarkGray, 0f, Vector2.Zero, new Vector2(barWidth, barHeight), SpriteEffects.None, 0f);
        spriteBatch.Draw(
            _pixel,
            barPosition,
            null,
            fillColor,
            0f,
            Vector2.Zero,
            new Vector2(barWidth * ratio, barHeight),
            SpriteEffects.None,
            0f);
    }

    private static SpriteAnimation GetAnimation(EnemySpriteSet sprites, EnemyAnimationClip clip) =>
        clip switch
        {
            EnemyAnimationClip.Run => sprites.Run,
            _ => sprites.Idle,
        };

    private static int RowForFacing(PlayerFacingDirection facing) =>
        facing switch
        {
            PlayerFacingDirection.East => 0,
            PlayerFacingDirection.SouthEast => 1,
            PlayerFacingDirection.South => 2,
            PlayerFacingDirection.SouthWest => 3,
            PlayerFacingDirection.West => 4,
            PlayerFacingDirection.NorthWest => 5,
            PlayerFacingDirection.North => 6,
            PlayerFacingDirection.NorthEast => 7,
            _ => 0,
        };

    private static PlayerFacingDirection ToFacing(Vector2 movement)
    {
        const float dead = 0.0001f;
        if (movement.LengthSquared() <= dead)
        {
            return PlayerFacingDirection.South;
        }

        var direction = Vector2.Normalize(movement);
        var angle = MathF.Atan2(direction.Y, direction.X);
        if (angle < 0f)
        {
            angle += MathF.Tau;
        }

        const float octantSize = MathF.PI / 4f;
        var octant = (int)MathF.Floor((angle + (octantSize * 0.5f)) / octantSize) % 8;

        return octant switch
        {
            0 => PlayerFacingDirection.East,
            1 => PlayerFacingDirection.SouthEast,
            2 => PlayerFacingDirection.South,
            3 => PlayerFacingDirection.SouthWest,
            4 => PlayerFacingDirection.West,
            5 => PlayerFacingDirection.NorthWest,
            6 => PlayerFacingDirection.North,
            _ => PlayerFacingDirection.NorthEast,
        };
    }

    private static SpriteAnimation BuildAnimation(Texture2D texture, int frameWidth, int frameHeight, float fps)
    {
        var columns = Math.Max(1, texture.Width / frameWidth);
        var rows = Math.Max(1, texture.Height / frameHeight);
        var frameDuration = fps <= 0f ? 0.1f : 1f / fps;
        return new SpriteAnimation(texture, frameWidth, frameHeight, columns, rows, frameDuration);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }

    private readonly record struct EnemySpriteKey(string Idle, string Run, int FrameSize);
}

