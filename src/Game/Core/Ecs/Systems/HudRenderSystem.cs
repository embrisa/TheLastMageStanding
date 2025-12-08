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
    private Texture2D _pixel = null!;
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
        _pixel = CreatePixel(graphicsDevice);
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
            var hasPauseMenu = world.TryGetComponent(_sessionEntity.Value, out PauseMenu pauseMenu);
            var hasAudioSettings = world.TryGetComponent(_sessionEntity.Value, out AudioSettingsState audioSettings);
            var hasAudioMenu = world.TryGetComponent(_sessionEntity.Value, out AudioSettingsMenu audioMenu);

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
                position.Y += 25f;

                // Draw XP bar and level
                DrawPlayerXpBar(world, spriteBatch, position);
            }

            // Draw game over overlay
            if (session.State == GameState.GameOver)
            {
                DrawGameOverOverlay(spriteBatch, session);
            }

            if (session.State == GameState.Paused)
            {
                if (hasAudioMenu && audioMenu.IsOpen)
                {
                    DrawAudioSettingsOverlay(
                        spriteBatch,
                        hasAudioSettings ? audioSettings : new AudioSettingsState(),
                        audioMenu);
                }
                else
                {
                    DrawPauseOverlay(
                        spriteBatch,
                        hasPauseMenu ? pauseMenu : new PauseMenu(0),
                        hasAudioSettings ? audioSettings : new AudioSettingsState());
                }
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
        spriteBatch.Draw(_pixel, screenRect, Color.Black * 0.7f);

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

    private void DrawPauseOverlay(SpriteBatch spriteBatch, PauseMenu pauseMenu, AudioSettingsState audioSettings)
    {
        var screenRect = new Rectangle(0, 0, 960, 540);
        spriteBatch.Draw(_pixel, screenRect, Color.Black * 0.65f);

        const float panelWidth = 360f;
        const float panelHeight = 260f;
        var panelRect = new Rectangle(
            (int)((screenRect.Width - panelWidth) * 0.5f),
            (int)((screenRect.Height - panelHeight) * 0.5f),
            (int)panelWidth,
            (int)panelHeight);
        spriteBatch.Draw(_pixel, panelRect, Color.Black * 0.85f);

        var title = "Paused";
        var titleSize = _titleFont.MeasureString(title);
        var titlePosition = new Vector2(
            panelRect.Center.X - titleSize.X / 2f,
            panelRect.Top + 24f);
        spriteBatch.DrawString(_titleFont, title, titlePosition, Color.White);

        var options = new[]
        {
            "Resume",
            "Restart Run",
            $"Audio Settings (Master {(int)(audioSettings.MasterVolume * 100)}%)",
            "Quit",
        };

        var selectedIndex = pauseMenu.SelectedIndex;
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        else if (selectedIndex >= options.Length)
        {
            selectedIndex = options.Length - 1;
        }

        var optionStartY = titlePosition.Y + titleSize.Y + 28f;
        var optionX = panelRect.Left + 40f;
        const float optionSpacing = 34f;

        for (var i = 0; i < options.Length; i++)
        {
            var isSelected = i == selectedIndex;
            var text = options[i];
            var textSize = _regularFont.MeasureString(text);
            var position = new Vector2(optionX, optionStartY + optionSpacing * i);

            if (isSelected)
            {
                var highlightRect = new Rectangle(
                    (int)(optionX - 16f),
                    (int)(position.Y - 6f),
                    (int)(panelWidth - 80f),
                    (int)(textSize.Y + 12f));
                spriteBatch.Draw(_pixel, highlightRect, Color.DarkGray * 0.8f);
            }

            var color = isSelected ? Color.Gold : Color.White;
            spriteBatch.DrawString(_regularFont, text, position, color);
        }
    }

    private void DrawAudioSettingsOverlay(SpriteBatch spriteBatch, AudioSettingsState audioSettings, AudioSettingsMenu audioMenu)
    {
        var screenRect = new Rectangle(0, 0, 960, 540);
        spriteBatch.Draw(_pixel, screenRect, Color.Black * 0.7f);

        const float panelWidth = 520f;
        const float panelHeight = 460f;
        var panelRect = new Rectangle(
            (int)((screenRect.Width - panelWidth) * 0.5f),
            (int)((screenRect.Height - panelHeight) * 0.5f),
            (int)panelWidth,
            (int)panelHeight);
        spriteBatch.Draw(_pixel, panelRect, Color.Black * 0.9f);

        var title = "Audio Settings";
        var titleSize = _titleFont.MeasureString(title);
        var titlePosition = new Vector2(
            panelRect.Center.X - titleSize.X / 2f,
            panelRect.Top + 20f);
        spriteBatch.DrawString(_titleFont, title, titlePosition, Color.White);

        var items = new (string Label, float? Value, bool? Toggle)[]
        {
            ("Master Volume", audioSettings.MasterVolume, null),
            ("Music Volume", audioSettings.MusicVolume, null),
            ("SFX Volume", audioSettings.SfxVolume, null),
            ("UI Volume", audioSettings.UiVolume, null),
            ("Voice Volume", audioSettings.VoiceVolume, null),
            ("Mute All", null, audioSettings.MuteAll),
            ("Master Mute", null, audioSettings.MasterMuted),
            ("Music Mute", null, audioSettings.MusicMuted),
            ("SFX Mute", null, audioSettings.SfxMuted),
            ("UI Mute", null, audioSettings.UiMuted),
            ("Voice Mute", null, audioSettings.VoiceMuted),
            ("Back", null, null),
        };

        var startY = titlePosition.Y + titleSize.Y + 24f;
        var rowHeight = 30f;
        var labelX = panelRect.Left + 28f;
        var sliderX = panelRect.Left + 240f;
        var sliderWidth = 200f;

        for (var i = 0; i < items.Length; i++)
        {
            var isSelected = i == audioMenu.SelectedIndex;
            var rowY = startY + rowHeight * i;
            var highlightRect = new Rectangle(
                (int)(labelX - 14f),
                (int)(rowY - 6f),
                (int)(panelWidth - 56f),
                (int)(rowHeight + 10f));

            if (isSelected)
            {
                spriteBatch.Draw(_pixel, highlightRect, Color.DarkSlateGray * 0.8f);
            }

            var label = items[i].Label;
            spriteBatch.DrawString(_regularFont, label, new Vector2(labelX, rowY), Color.White);

            if (items[i].Value is float sliderValue)
            {
                DrawSliderBar(spriteBatch, new Vector2(sliderX, rowY + 4f), sliderValue, sliderWidth);
                var percentText = $"{(int)(sliderValue * 100)}%";
                var percentSize = _regularFont.MeasureString(percentText);
                spriteBatch.DrawString(
                    _regularFont,
                    percentText,
                    new Vector2(sliderX + sliderWidth + 12f, rowY),
                    isSelected ? Color.Gold : Color.LightGray);
            }
            else if (items[i].Toggle is bool toggle)
            {
                var toggleText = toggle ? "Muted" : "On";
                spriteBatch.DrawString(
                    _regularFont,
                    toggleText,
                    new Vector2(sliderX, rowY),
                    toggle ? Color.OrangeRed : Color.LightGreen);
            }
        }

        var hintText = "Up/Down: select    Left/Right: adjust    Enter: toggle    Esc: back";
        var hintSize = _regularFont.MeasureString(hintText);
        var hintPosition = new Vector2(
            panelRect.Center.X - hintSize.X / 2f,
            panelRect.Bottom - hintSize.Y - 12f);
        spriteBatch.DrawString(_regularFont, hintText, hintPosition, Color.LightGray);

        if (!string.IsNullOrEmpty(audioMenu.ConfirmationText))
        {
            var alpha = MathHelper.Clamp(audioMenu.ConfirmationTimerSeconds / 1.0f, 0f, 1f);
            var textSize = _regularFont.MeasureString(audioMenu.ConfirmationText);
            var textPosition = new Vector2(
                panelRect.Center.X - textSize.X / 2f,
                panelRect.Bottom - textSize.Y - 48f);
            spriteBatch.DrawString(_regularFont, audioMenu.ConfirmationText, textPosition, Color.Gold * alpha);
        }
    }

    private void DrawSliderBar(SpriteBatch spriteBatch, Vector2 position, float value, float width)
    {
        const float height = 8f;
        spriteBatch.Draw(
            _pixel,
            position,
            null,
            Color.DimGray,
            0f,
            Vector2.Zero,
            new Vector2(width, height),
            SpriteEffects.None,
            0f);

        var fillColor = Color.Lerp(Color.DarkSeaGreen, Color.Gold, value);
        spriteBatch.Draw(
            _pixel,
            position,
            null,
            fillColor,
            0f,
            Vector2.Zero,
            new Vector2(width * MathHelper.Clamp(value, 0f, 1f), height),
            SpriteEffects.None,
            0f);
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

    private void DrawPlayerXpBar(EcsWorld world, SpriteBatch spriteBatch, Vector2 position)
    {
        // Find player with XP component
        world.ForEach<PlayerTag, PlayerXp>(
            (Entity entity, ref PlayerTag _, ref PlayerXp playerXp) =>
            {
                // Draw level label
                var levelText = $"Level {playerXp.Level}";
                spriteBatch.DrawString(_regularFont, levelText, position, Color.White);

                // Move down for XP bar
                var barPosition = position + new Vector2(0f, 25f);
                const float barWidth = 180f;
                const float barHeight = 8f;

                // Calculate XP ratio
                var xpRatio = playerXp.XpToNextLevel > 0
                    ? MathHelper.Clamp((float)playerXp.CurrentXp / playerXp.XpToNextLevel, 0f, 1f)
                    : 0f;

                // Draw background
                spriteBatch.Draw(
                    _pixel,
                    barPosition,
                    null,
                    Color.DarkGray,
                    0f,
                    Vector2.Zero,
                    new Vector2(barWidth, barHeight),
                    SpriteEffects.None,
                    0f);

                // Draw fill
                var fillColor = Color.Lerp(Color.Yellow, Color.Gold, xpRatio);
                spriteBatch.Draw(
                    _pixel,
                    barPosition,
                    null,
                    fillColor,
                    0f,
                    Vector2.Zero,
                    new Vector2(barWidth * xpRatio, barHeight),
                    SpriteEffects.None,
                    0f);

                // Draw XP text
                var xpText = $"{playerXp.CurrentXp}/{playerXp.XpToNextLevel}";
                var xpTextSize = _regularFont.MeasureString(xpText);
                var xpTextPosition = barPosition + new Vector2(barWidth + 10f, -4f);
                spriteBatch.DrawString(_regularFont, xpText, xpTextPosition, Color.White);
            });
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }
}
