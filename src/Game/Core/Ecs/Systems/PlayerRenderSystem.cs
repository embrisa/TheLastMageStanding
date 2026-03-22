using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class PlayerRenderSystem : IDrawSystem, ILoadContentSystem
{
    private Texture2D? _pixel;
    private PlayerSpriteSet _sprites;
    private PlayerVisual _visual;
    private bool _contentLoaded;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _pixel ??= CreatePixel(graphicsDevice);

        var idle = content.Load<Texture2D>("Sprites/player/Idle");
        var run = content.Load<Texture2D>("Sprites/player/Run");
        var runBackwards = content.Load<Texture2D>("Sprites/player/RunBackwards");
        var strafeLeft = content.Load<Texture2D>("Sprites/player/StrafeLeft");
        var strafeRight = content.Load<Texture2D>("Sprites/player/StrafeRight");
        var hit = content.Load<Texture2D>("Sprites/player/TakeDamage");

        const int frameSize = 128;
        _sprites = new PlayerSpriteSet
        {
            Idle = BuildAnimation(idle, frameSize, frameSize, fps: 6f),
            Run = BuildAnimation(run, frameSize, frameSize, fps: 12f),
            RunBackwards = BuildAnimation(runBackwards, frameSize, frameSize, fps: 12f),
            StrafeLeft = BuildAnimation(strafeLeft, frameSize, frameSize, fps: 12f),
            StrafeRight = BuildAnimation(strafeRight, frameSize, frameSize, fps: 12f),
            Hit = BuildAnimation(hit, frameSize, frameSize, fps: 10f),
        };

        var origin = new Vector2(frameSize * 0.5f, frameSize * 0.88f);
        _visual = new PlayerVisual(origin, scale: 1f, frameSize);

        world.ForEach<PlayerTag>(
            (Entity entity, ref PlayerTag _) =>
            {
                if (!world.TryGetComponent(entity, out PlayerSpriteSet _))
                {
                    world.SetComponent(entity, _sprites);
                }

                if (!world.TryGetComponent(entity, out PlayerVisual _))
                {
                    world.SetComponent(entity, _visual);
                }

                if (!world.TryGetComponent(entity, out PlayerAnimationState _))
                {
                    world.SetComponent(
                        entity,
                        new PlayerAnimationState
                        {
                            Facing = PlayerFacingDirection.South,
                            ActiveClip = PlayerAnimationClip.Idle,
                            Timer = 0f,
                            FrameIndex = 0,
                            IsMoving = false,
                        });
                }
            });

        _contentLoaded = true;
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_contentLoaded || _pixel is null)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;
        world.ForEach<Position, PlayerAnimationState, PlayerSpriteSet>(
            (Entity entity, ref Position position, ref PlayerAnimationState state, ref PlayerSpriteSet sprites) =>
            {
                if (!world.TryGetComponent(entity, out PlayerVisual visual))
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

                var tint = Color.White;
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

                DrawHealthBar(world, entity, spriteBatch, position.Value, visual);
            });
    }

    private void DrawHealthBar(EcsWorld world, Entity entity, SpriteBatch spriteBatch, Vector2 position, PlayerVisual visual)
    {
        if (!world.TryGetComponent(entity, out Health health))
        {
            return;
        }

        const float barWidth = 38f;
        const float barHeight = 5f;
        var offsetY = -visual.Origin.Y - 12f;
        var barPosition = position + new Vector2(-barWidth * 0.5f, offsetY);
        var ratio = MathHelper.Clamp(health.Ratio, 0f, 1f);
        var fillColor = Color.Lerp(Color.Red, Color.LimeGreen, ratio);

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

    private static SpriteAnimation BuildAnimation(Texture2D texture, int frameWidth, int frameHeight, float fps)
    {
        var columns = Math.Max(1, texture.Width / frameWidth);
        var rows = Math.Max(1, texture.Height / frameHeight);
        var frameDuration = fps <= 0f ? 0.1f : 1f / fps;
        return new SpriteAnimation(texture, frameWidth, frameHeight, columns, rows, frameDuration);
    }

    internal static PlayerAnimationClip ClipForFacing(PlayerFacingDirection facing) =>
        facing switch
        {
            PlayerFacingDirection.North => PlayerAnimationClip.RunBackwards,
            PlayerFacingDirection.NorthEast => PlayerAnimationClip.RunBackwards,
            PlayerFacingDirection.NorthWest => PlayerAnimationClip.RunBackwards,
            PlayerFacingDirection.West => PlayerAnimationClip.Run,
            PlayerFacingDirection.East => PlayerAnimationClip.Run,
            PlayerFacingDirection.South => PlayerAnimationClip.Run,
            PlayerFacingDirection.SouthEast => PlayerAnimationClip.Run,
            PlayerFacingDirection.SouthWest => PlayerAnimationClip.Run,
            _ => PlayerAnimationClip.Run,
        };

    internal static SpriteAnimation GetAnimation(PlayerSpriteSet sprites, PlayerAnimationClip clip) =>
        clip switch
        {
            PlayerAnimationClip.Run => sprites.Run,
            PlayerAnimationClip.RunBackwards => sprites.RunBackwards,
            PlayerAnimationClip.StrafeLeft => sprites.StrafeLeft,
            PlayerAnimationClip.StrafeRight => sprites.StrafeRight,
            PlayerAnimationClip.Hit => sprites.Hit,
            _ => sprites.Idle,
        };

    internal static PlayerFacingDirection ToFacing(Vector2 movement)
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

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}
