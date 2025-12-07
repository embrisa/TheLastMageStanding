using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders HUD elements in screen space: current wave info, transient notifications,
/// and game over overlay.
/// </summary>
internal sealed class HudRenderSystem : IDrawSystem, ILoadContentSystem
{
    private SpriteFont _regularFont = null!;
    private SpriteFont _titleFont = null!;
    private Entity? _sessionEntity;
    private Entity? _notificationEntity;

    public void Initialize(EcsWorld world)
    {
        // No event subscriptions needed
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _regularFont = content.Load<SpriteFont>("Fonts/FontRegularText");
        _titleFont = content.Load<SpriteFont>("Fonts/FontRegularTitle");
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        var spriteBatch = context.SpriteBatch;

        // Find session entity
        if (_sessionEntity is null || !world.IsAlive(_sessionEntity.Value))
        {
            world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
            {
                _sessionEntity = entity;
            });
        }

        // Find notification entity
        if (_notificationEntity is null || !world.IsAlive(_notificationEntity.Value))
        {
            _notificationEntity = null;
            world.ForEach<WaveNotification>((Entity entity, ref WaveNotification _) =>
            {
                _notificationEntity = entity;
            });
        }

        // Get session state
        if (_sessionEntity.HasValue && world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            // Draw HUD in top-left corner (only if playing)
            if (session.State == GameState.Playing)
            {
                var timeSpan = System.TimeSpan.FromSeconds(session.TimeSurvived);
                var timeText = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
                var waveText = $"Wave {session.CurrentWave}";
                var killsText = $"Kills: {session.EnemiesKilled}";

                var position = new Vector2(20f, 20f);
                spriteBatch.DrawString(_regularFont, timeText, position, Color.White);
                position.Y += 25f;
                spriteBatch.DrawString(_regularFont, waveText, position, Color.White);
                position.Y += 25f;
                spriteBatch.DrawString(_regularFont, killsText, position, Color.White);
            }

            // Draw game over overlay
            if (session.State == GameState.GameOver)
            {
                DrawGameOverOverlay(spriteBatch, session);
            }
        }

        // Draw notification (centered)
        if (_notificationEntity.HasValue && world.TryGetComponent<WaveNotification>(_notificationEntity.Value, out var notification))
        {
            DrawNotification(spriteBatch, notification);
        }
    }

    private void DrawGameOverOverlay(SpriteBatch spriteBatch, GameSession session)
    {
        // Draw semi-transparent background
        var screenRect = new Rectangle(0, 0, 960, 540);
        var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        spriteBatch.Draw(pixel, screenRect, Color.Black * 0.7f);

        // Draw "GAME OVER" text centered
        var gameOverText = "GAME OVER";
        var gameOverSize = _titleFont.MeasureString(gameOverText);
        var gameOverPosition = new Vector2(
            960f / 2f - gameOverSize.X / 2f,
            540f / 2f - gameOverSize.Y / 2f - 60f
        );
        spriteBatch.DrawString(_titleFont, gameOverText, gameOverPosition, Color.Red);

        // Draw stats summary
        var timeSpan = System.TimeSpan.FromSeconds(session.TimeSurvived);
        var summaryText = $"Survived: {(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}   Wave: {session.CurrentWave}   Kills: {session.EnemiesKilled}";
        var summarySize = _regularFont.MeasureString(summaryText);
        var summaryPosition = new Vector2(
            960f / 2f - summarySize.X / 2f,
            540f / 2f - summarySize.Y / 2f
        );
        spriteBatch.DrawString(_regularFont, summaryText, summaryPosition, Color.White);

        // Draw restart instruction centered below
        var restartText = "Press R or Enter to Restart";
        var restartSize = _regularFont.MeasureString(restartText);
        var restartPosition = new Vector2(
            960f / 2f - restartSize.X / 2f,
            540f / 2f - restartSize.Y / 2f + 60f
        );
        spriteBatch.DrawString(_regularFont, restartText, restartPosition, Color.White);
    }

    private void DrawNotification(SpriteBatch spriteBatch, WaveNotification notification)
    {
        // Skip game over notifications (handled by overlay)
        if (notification.RemainingSeconds == float.MaxValue)
            return;

        // Calculate fade based on remaining time
        var alpha = MathHelper.Clamp(notification.RemainingSeconds / 0.5f, 0f, 1f);
        var color = Color.White * alpha;

        // Draw centered at top-center
        var size = _titleFont.MeasureString(notification.Message);
        var position = new Vector2(
            960f / 2f - size.X / 2f,
            100f
        );
        spriteBatch.DrawString(_titleFont, notification.Message, position, color);
    }
}
