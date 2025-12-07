using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class PlayerRenderSystem : IUpdateSystem, IDrawSystem, ILoadContentSystem
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

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_contentLoaded)
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<PlayerAnimationState, PlayerSpriteSet, Velocity>(
            (Entity entity, ref PlayerAnimationState state, ref PlayerSpriteSet sprites, ref Velocity velocity) =>
            {
                var hitActive = false;

                if (world.TryGetComponent(entity, out PlayerHitState hitState))
                {
                    hitActive = hitState.RemainingSeconds > 0f;

                    hitState.RemainingSeconds -= deltaSeconds;
                    if (hitState.RemainingSeconds <= 0f)
                    {
                        world.RemoveComponent<PlayerHitState>(entity);
                        hitActive = false;
                    }
                    else
                    {
                        world.SetComponent(entity, hitState);
                    }
                }

                if (hitActive)
                {
                    // Update facing based on current input during hit animation
                    if (world.TryGetComponent(entity, out InputIntent intent))
                    {
                        var movement = intent.Movement;
                        if (movement.LengthSquared() > 0.0001f)
                        {
                            state.Facing = ToFacing(movement);
                        }
                    }

                    if (state.ActiveClip != PlayerAnimationClip.Hit)
                    {
                        state.ActiveClip = PlayerAnimationClip.Hit;
                        state.Timer = 0f;
                        state.FrameIndex = 0;
                    }

                    var hitAnimation = GetAnimation(sprites, PlayerAnimationClip.Hit);
                    var frameDuration = hitAnimation.FrameDurationSeconds <= 0f ? 0.1f : hitAnimation.FrameDurationSeconds;
                    var frameCount = Math.Max(1, hitAnimation.Columns);

                    state.Timer += deltaSeconds;
                    while (state.Timer >= frameDuration)
                    {
                        state.Timer -= frameDuration;
                        state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                    }

                    state.IsMoving = false;
                }
                else
                {
                    var movement = velocity.Value;
                    var isMoving = movement.LengthSquared() > 0.0001f;
                    var facing = isMoving ? ToFacing(movement) : state.Facing;
                    var clip = isMoving ? ClipForFacing(facing) : PlayerAnimationClip.Idle;

                    if (clip != state.ActiveClip)
                    {
                        state.ActiveClip = clip;
                        state.FrameIndex = 0;
                        state.Timer = 0f;
                    }

                    var animation = GetAnimation(sprites, state.ActiveClip);
                    var frameDuration = animation.FrameDurationSeconds <= 0f ? 0.1f : animation.FrameDurationSeconds;
                    var frameCount = Math.Max(1, animation.Columns);

                    state.Timer += deltaSeconds;
                    while (state.Timer >= frameDuration)
                    {
                        state.Timer -= frameDuration;
                        state.FrameIndex = (state.FrameIndex + 1) % frameCount;
                    }

                    state.IsMoving = isMoving;
                    state.Facing = facing;
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
            PlayerFacingDirection.East => 0,        // Row1
            PlayerFacingDirection.SouthEast => 1,   // Row2
            PlayerFacingDirection.South => 2,       // Row3
            PlayerFacingDirection.SouthWest => 3,   // Row4
            PlayerFacingDirection.West => 4,        // Row5
            PlayerFacingDirection.NorthWest => 5,   // Row6
            PlayerFacingDirection.North => 6,       // Row7
            PlayerFacingDirection.NorthEast => 7,   // Row8
            _ => 0,
        };

    private static SpriteAnimation BuildAnimation(Texture2D texture, int frameWidth, int frameHeight, float fps)
    {
        var columns = Math.Max(1, texture.Width / frameWidth);
        var rows = Math.Max(1, texture.Height / frameHeight);
        var frameDuration = fps <= 0f ? 0.1f : 1f / fps;
        return new SpriteAnimation(texture, frameWidth, frameHeight, columns, rows, frameDuration);
    }

    private static PlayerAnimationClip ClipForFacing(PlayerFacingDirection facing) =>
        facing switch
        {
            PlayerFacingDirection.North => PlayerAnimationClip.RunBackwards,
            PlayerFacingDirection.NorthEast => PlayerAnimationClip.RunBackwards,
            PlayerFacingDirection.NorthWest => PlayerAnimationClip.RunBackwards,
            // Use the main run set for east/west to keep facing consistent with WASD rows.
            PlayerFacingDirection.West => PlayerAnimationClip.Run,
            PlayerFacingDirection.East => PlayerAnimationClip.Run,
            PlayerFacingDirection.SouthEast => PlayerAnimationClip.Run,
            PlayerFacingDirection.SouthWest => PlayerAnimationClip.Run,
            _ => PlayerAnimationClip.Run,
        };

    private static SpriteAnimation GetAnimation(PlayerSpriteSet sprites, PlayerAnimationClip clip) =>
        clip switch
        {
            PlayerAnimationClip.Run => sprites.Run,
            PlayerAnimationClip.RunBackwards => sprites.RunBackwards,
            PlayerAnimationClip.StrafeLeft => sprites.StrafeLeft,
            PlayerAnimationClip.StrafeRight => sprites.StrafeRight,
            PlayerAnimationClip.Hit => sprites.Hit,
            _ => sprites.Idle,
        };

    private static PlayerFacingDirection ToFacing(Vector2 movement)
    {
        const float dead = 0.0001f;
        if (movement.LengthSquared() <= dead)
        {
            return PlayerFacingDirection.South;
        }

        var direction = Vector2.Normalize(movement);
        var angle = MathF.Atan2(direction.Y, direction.X); // y > 0 is down in screen space
        if (angle < 0f)
        {
            angle += MathF.Tau;
        }

        const float octantSize = MathF.PI / 4f; // 45 degrees per facing slice
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


