using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Rendering.UI;

/// <summary>
/// Renders the skill hotbar UI showing equipped skills, cooldown timers, and hotkey bindings.
/// </summary>
internal sealed class SkillHotbarRenderer : IUiDrawSystem, ILoadContentSystem
{
    private const int SlotCount = 5;
    private const int SlotSize = 48;
    private const int SlotSpacing = 8;
    private const int BottomMargin = 80;
    private const int CastBarWidth = 200;
    private const int CastBarHeight = 6;
    private const int CastBarTopMargin = 12;

    private readonly SkillRegistry _skillRegistry;
    private Texture2D _pixel = null!;
    private SpriteFont _hotkeyFont = null!;
    private Dictionary<SkillId, Texture2D> _skillIcons = null!;
    private Entity? _playerEntity;

    public SkillHotbarRenderer(SkillRegistry skillRegistry)
    {
        _skillRegistry = skillRegistry;
    }

    public void Initialize(EcsWorld world)
    {
        // No event subscriptions needed
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _pixel = CreatePixel(graphicsDevice);
        _hotkeyFont = content.Load<SpriteFont>("Fonts/FontRegularText");
        
        // Load skill icons
        _skillIcons = new Dictionary<SkillId, Texture2D>();
        LoadSkillIcon(content, SkillId.Firebolt, "Sprites/icons/abilities/Firebolt");
        LoadSkillIcon(content, SkillId.Fireball, "Sprites/icons/abilities/Fireball");
        LoadSkillIcon(content, SkillId.FlameWave, "Sprites/icons/abilities/FlameWave");
        LoadSkillIcon(content, SkillId.ArcaneMissile, "Sprites/icons/abilities/ArcaneMissile");
        LoadSkillIcon(content, SkillId.ArcaneBurst, "Sprites/icons/abilities/ArcaneBurst");
        LoadSkillIcon(content, SkillId.ArcaneBarrage, "Sprites/icons/abilities/ArcaneBarrage");
        LoadSkillIcon(content, SkillId.FrostBolt, "Sprites/icons/abilities/FrostBolt");
        LoadSkillIcon(content, SkillId.FrostNova, "Sprites/icons/abilities/FrostNova");
        LoadSkillIcon(content, SkillId.Blizzard, "Sprites/icons/abilities/Blizzard");
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        var spriteBatch = context.SpriteBatch;

        // Find player entity
        if (_playerEntity is null || !world.IsAlive(_playerEntity.Value))
        {
            world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
            {
                _playerEntity = entity;
            });
        }

        if (!_playerEntity.HasValue)
        {
            return;
        }

        var player = _playerEntity.Value;

        // Get player skill components
        if (!world.TryGetComponent(player, out EquippedSkills equipped) ||
            !world.TryGetComponent(player, out SkillCooldowns cooldowns))
        {
            return;
        }

        // Calculate hotbar position (bottom-center)
        var hotbarWidth = (SlotSize * SlotCount) + (SlotSpacing * (SlotCount - 1));
        var hotbarX = (960f - hotbarWidth) / 2f;
        var hotbarY = 540f - BottomMargin;

        // Draw cast progress bar if casting
        if (world.TryGetComponent(player, out SkillCasting casting))
        {
            DrawCastProgressBar(spriteBatch, hotbarX, hotbarY, casting);
        }

        // Draw each skill slot
        for (var i = 0; i < SlotCount; i++)
        {
            var slotX = hotbarX + (i * (SlotSize + SlotSpacing));
            var slotY = hotbarY;
            
            var skillId = equipped.GetSkill(i);
            var cooldown = cooldowns.GetCooldown(skillId);
            
            DrawSkillSlot(spriteBatch, slotX, slotY, i, skillId, cooldown);
        }
    }

    private void DrawSkillSlot(SpriteBatch spriteBatch, float x, float y, int slot, SkillId skillId, float cooldownRemaining)
    {
        var slotRect = new Rectangle((int)x, (int)y, SlotSize, SlotSize);

        // Draw slot background
        var backgroundColor = skillId == SkillId.None ? new Color(40, 40, 40, 200) : new Color(60, 60, 60, 200);
        spriteBatch.Draw(_pixel, slotRect, backgroundColor);

        // Draw skill icon if equipped
        if (skillId != SkillId.None && _skillIcons.TryGetValue(skillId, out var icon))
        {
            var iconRect = new Rectangle((int)x + 8, (int)y + 8, 32, 32);
            spriteBatch.Draw(icon, iconRect, Color.White);
        }

        // Draw cooldown overlay
        if (cooldownRemaining > 0.1f)
        {
            var skillDef = _skillRegistry.GetSkill(skillId);
            if (skillDef != null)
            {
                var cooldownPercent = cooldownRemaining / skillDef.BaseCooldown;
                DrawCooldownOverlay(spriteBatch, slotRect, cooldownPercent);
            }
        }

        // Draw slot border
        DrawRectangleBorder(spriteBatch, slotRect, 2, new Color(100, 100, 100, 255));

        // Draw hotkey label
        var hotkeyLabel = slot == 0 ? "LMB" : slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var labelSize = _hotkeyFont.MeasureString(hotkeyLabel);
        var labelPosition = new Vector2(
            x + SlotSize - labelSize.X - 4,
            y + SlotSize - labelSize.Y - 2
        );
        
        // Draw label shadow
        spriteBatch.DrawString(_hotkeyFont, hotkeyLabel, labelPosition + new Vector2(1, 1), Color.Black);
        spriteBatch.DrawString(_hotkeyFont, hotkeyLabel, labelPosition, Color.White);
    }

    private void DrawCooldownOverlay(SpriteBatch spriteBatch, Rectangle slotRect, float percent)
    {
        // Simple top-to-bottom fill overlay
        var overlayHeight = (int)(slotRect.Height * percent);
        var overlayRect = new Rectangle(
            slotRect.X,
            slotRect.Y,
            slotRect.Width,
            overlayHeight
        );
        
        spriteBatch.Draw(_pixel, overlayRect, new Color(20, 20, 20, 180));
    }

    private void DrawCastProgressBar(SpriteBatch spriteBatch, float hotbarX, float hotbarY, SkillCasting casting)
    {
        var barX = (960f - CastBarWidth) / 2f;
        var barY = hotbarY - CastBarTopMargin - CastBarHeight;

        // Draw background
        var bgRect = new Rectangle((int)barX, (int)barY, CastBarWidth, CastBarHeight);
        spriteBatch.Draw(_pixel, bgRect, new Color(40, 40, 40, 200));

        // Draw progress fill
        var progress = casting.CastProgress;
        var fillWidth = (int)(CastBarWidth * progress);
        var fillRect = new Rectangle((int)barX, (int)barY, fillWidth, CastBarHeight);
        
        // Get element color
        var skillDef = _skillRegistry.GetSkill(casting.SkillId);
        var fillColor = GetElementColor(skillDef?.Element ?? SkillElement.None);
        spriteBatch.Draw(_pixel, fillRect, fillColor);

        // Draw border
        DrawRectangleBorder(spriteBatch, bgRect, 1, new Color(100, 100, 100, 255));
    }

    private static Color GetElementColor(SkillElement element)
    {
        return element switch
        {
            SkillElement.Fire => new Color(255, 80, 40),
            SkillElement.Arcane => new Color(200, 100, 255),
            SkillElement.Frost => new Color(100, 200, 255),
            _ => Color.White
        };
    }

    private void DrawRectangleBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        // Top
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void LoadSkillIcon(ContentManager content, SkillId skillId, string path)
    {
        try
        {
            _skillIcons[skillId] = content.Load<Texture2D>(path);
        }
        catch (ContentLoadException)
        {
            Console.WriteLine($"[SkillHotbarRenderer] Warning: Could not load icon for {skillId} at {path}");
        }
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }
}
