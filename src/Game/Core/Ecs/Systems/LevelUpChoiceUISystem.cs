using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles navigation, application, and rendering of level-up choices.
/// </summary>
[SuppressMessage("Design", "CA1001", Justification = "Texture lifetime matches graphics device; disposed on game shutdown.")]
internal sealed class LevelUpChoiceUISystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem
{
    private readonly LevelUpChoiceGenerator _generator;
    private SpriteFont _regularFont = null!;
    private SpriteFont _titleFont = null!;
    private Texture2D _pixel = null!;
    private Entity? _sessionEntity;

    public LevelUpChoiceUISystem(LevelUpChoiceGenerator generator)
    {
        _generator = generator;
    }

    public void Initialize(EcsWorld world)
    {
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => ClearState(world));
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _regularFont = content.Load<SpriteFont>("Fonts/FontRegularText");
        _titleFont = content.Load<SpriteFont>("Fonts/FontRegularTitle");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryGetChoiceState(world, out var sessionEntity, out var state) || !state.IsOpen)
        {
            return;
        }

        var choices = state.Choices;
        if (choices is null || choices.Count == 0)
        {
            CloseAndResume(world, sessionEntity, ref state);
            return;
        }

        var choiceCount = choices.Count;
        state.SelectedIndex = Math.Clamp(state.SelectedIndex, 0, choiceCount - 1);

        if (context.Input.MenuLeftPressed)
        {
            state.SelectedIndex = (state.SelectedIndex - 1 + choiceCount) % choiceCount;
        }

        if (context.Input.MenuRightPressed)
        {
            state.SelectedIndex = (state.SelectedIndex + 1) % choiceCount;
        }

        if (context.Input.MenuConfirmPressed)
        {
            ApplyChoice(world, state.Player, choices[state.SelectedIndex]);

            if (state.PendingLevels > 0)
            {
                state.PendingLevels--;
                state.Choices = _generator.GenerateChoices(world, state.Player);
                state.SelectedIndex = 0;
                state.IsOpen = true;
                EnsurePaused(world, sessionEntity);
            }
            else
            {
                CloseAndResume(world, sessionEntity, ref state);
            }

            world.SetComponent(sessionEntity, state);
            return;
        }

        world.SetComponent(sessionEntity, state);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!TryGetChoiceState(world, out _, out var state) || !state.IsOpen)
        {
            return;
        }

        var choices = state.Choices;
        if (choices is null || choices.Count == 0)
        {
            return;
        }

        var spriteBatch = context.SpriteBatch;
        var screenRect = new Rectangle(0, 0, 960, 540);

        // Dim background
        spriteBatch.Draw(_pixel, screenRect, Color.Black * 0.65f);

        var title = "Level Up! Choose one of 3 options";
        var titleSize = _titleFont.MeasureString(title);
        var titlePos = new Vector2(
            screenRect.Center.X - titleSize.X / 2f,
            48f);
        spriteBatch.DrawString(_titleFont, title, titlePos, Color.Gold);

        var subtitle = "Navigate with A/D or Arrow Keys. Confirm with Enter/Space.";
        var subtitleSize = _regularFont.MeasureString(subtitle);
        var subtitlePos = new Vector2(
            screenRect.Center.X - subtitleSize.X / 2f,
            titlePos.Y + titleSize.Y + 6f);
        spriteBatch.DrawString(_regularFont, subtitle, subtitlePos, Color.LightGray);

        const float cardWidth = 260f;
        const float cardHeight = 200f;
        const float cardSpacing = 20f;
        var totalWidth = choices.Count * cardWidth + (choices.Count - 1) * cardSpacing;
        var startX = (screenRect.Width - totalWidth) * 0.5f;
        var cardY = 150f;

        for (var i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            var cardRect = new Rectangle(
                (int)(startX + i * (cardWidth + cardSpacing)),
                (int)cardY,
                (int)cardWidth,
                (int)cardHeight);

            var isSelected = i == state.SelectedIndex;
            var backgroundColor = isSelected ? Color.DarkSlateBlue * 0.9f : Color.Black * 0.7f;
            var borderColor = isSelected ? Color.Gold : Color.Gray;

            // Card background
            spriteBatch.Draw(_pixel, cardRect, backgroundColor);

            // Border
            DrawBorder(spriteBatch, cardRect, 2, borderColor);

            // Title
            var titleText = choice.Title;
            var titleMeasure = _regularFont.MeasureString(titleText);
            var titlePosition = new Vector2(
                cardRect.Center.X - titleMeasure.X / 2f,
                cardRect.Top + 14f);
            spriteBatch.DrawString(_regularFont, titleText, titlePosition, Color.White);

            // Description (wrapped)
            var descriptionText = WrapText(_regularFont, choice.Description, cardWidth - 24f);
            var descriptionPosition = new Vector2(
                cardRect.Left + 12f,
                titlePosition.Y + titleMeasure.Y + 12f);
            spriteBatch.DrawString(_regularFont, descriptionText, descriptionPosition, Color.LightGray);

            // Footer label
            var footer = choice.Kind == LevelUpChoiceKind.StatBoost ? "Stat Boost" : "Skill Modifier";
            var footerSize = _regularFont.MeasureString(footer);
            var footerPos = new Vector2(
                cardRect.Center.X - footerSize.X / 2f,
                cardRect.Bottom - footerSize.Y - 12f);
            spriteBatch.DrawString(_regularFont, footer, footerPos, Color.WhiteSmoke);
        }
    }

    private void ApplyChoice(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        switch (choice.Kind)
        {
            case LevelUpChoiceKind.StatBoost:
                ApplyStatBoost(world, player, choice);
                break;
            case LevelUpChoiceKind.SkillModifier:
                ApplySkillModifier(world, player, choice);
                break;
        }

        AppendHistory(world, choice);
    }

    private static void ApplyStatBoost(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        switch (choice.StatType)
        {
            case StatBoostType.MaxHealth:
                if (world.TryGetComponent(player, out Health health))
                {
                    var ratio = health.Ratio;
                    health.Max += choice.StatAmount;
                    health.Current = health.Max * ratio;
                    world.SetComponent(player, health);
                }
                break;
            case StatBoostType.AttackDamage:
                if (world.TryGetComponent(player, out AttackStats attackStats))
                {
                    attackStats.Damage += choice.StatAmount;
                    world.SetComponent(player, attackStats);
                }
                break;
            case StatBoostType.MoveSpeed:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.MoveSpeedAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.Armor:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.ArmorAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.Power:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.PowerAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
            case StatBoostType.CritChance:
            {
                var modifiers = GetLevelUpModifiers(world, player);
                modifiers.CritChanceAdditive += choice.StatAmount;
                SaveLevelUpModifiers(world, player, modifiers);
                break;
            }
        }

        MarkStatsDirty(world, player);
    }

    private static void ApplySkillModifier(EcsWorld world, Entity player, LevelUpChoice choice)
    {
        if (!world.TryGetComponent(player, out PlayerSkillModifiers modifiers))
        {
            modifiers = new PlayerSkillModifiers();
        }

        modifiers.SkillSpecificModifiers ??= new Dictionary<SkillId, SkillModifiers>();
        modifiers.ElementModifiers ??= new Dictionary<SkillElement, SkillModifiers>();

        modifiers.SkillSpecificModifiers.TryGetValue(choice.SkillId, out var skillMods);

        switch (choice.SkillModifierType)
        {
            case SkillModifierType.DamagePercent:
                skillMods.DamageMultiplicative *= 1f + choice.SkillModifierAmount;
                break;
            case SkillModifierType.CooldownReductionPercent:
                skillMods.CooldownReductionAdditive += choice.SkillModifierAmount;
                break;
            case SkillModifierType.AoePercent:
                skillMods.AoeRadiusMultiplicative *= 1f + choice.SkillModifierAmount;
                break;
            case SkillModifierType.ProjectileCount:
                skillMods.ProjectileCountAdditive += choice.SkillModifierIntAmount;
                break;
            case SkillModifierType.PierceCount:
                skillMods.PierceCountAdditive += choice.SkillModifierIntAmount;
                break;
            case SkillModifierType.CastSpeedPercent:
                skillMods.CastTimeReductionAdditive += choice.SkillModifierAmount;
                break;
        }

        modifiers.SkillSpecificModifiers[choice.SkillId] = skillMods;
        PlayerSkillModifiers.MarkDirty(ref modifiers);
        world.SetComponent(player, modifiers);
    }

    private static StatModifiers GetLevelUpModifiers(EcsWorld world, Entity player)
    {
        if (!world.TryGetComponent(player, out LevelUpStatModifiers levelUpMods))
        {
            levelUpMods = new LevelUpStatModifiers { Value = StatModifiers.Zero };
        }

        return levelUpMods.Value;
    }

    private static void SaveLevelUpModifiers(EcsWorld world, Entity player, StatModifiers modifiers)
    {
        world.SetComponent(player, new LevelUpStatModifiers { Value = modifiers });
    }

    private static void MarkStatsDirty(EcsWorld world, Entity player)
    {
        if (world.TryGetComponent(player, out ComputedStats computed))
        {
            computed.IsDirty = true;
            world.SetComponent(player, computed);
        }
    }

    private void AppendHistory(EcsWorld world, LevelUpChoice choice)
    {
        var sessionEntity = EnsureSessionEntity(world);
        if (!sessionEntity.HasValue)
        {
            return;
        }

        var history = world.TryGetComponent(sessionEntity.Value, out LevelUpChoiceHistory existing)
            ? existing
            : new LevelUpChoiceHistory { Selections = new List<string>() };

        history.Selections ??= new List<string>();
        history.Selections.Add(choice.Title);
        world.SetComponent(sessionEntity.Value, history);
    }

    private static void CloseAndResume(EcsWorld world, Entity sessionEntity, ref LevelUpChoiceState state)
    {
        state.IsOpen = false;
        state.SelectedIndex = 0;
        state.Choices = new List<LevelUpChoice>();
        state.PendingLevels = 0;
        world.SetComponent(sessionEntity, state);

        if (world.TryGetComponent(sessionEntity, out GameSession session) &&
            session.State == GameState.Paused)
        {
            session.State = GameState.Playing;
            world.SetComponent(sessionEntity, session);
        }
    }

    private static void EnsurePaused(EcsWorld world, Entity sessionEntity)
    {
        if (world.TryGetComponent(sessionEntity, out GameSession session) &&
            session.State != GameState.GameOver)
        {
            session.State = GameState.Paused;
            world.SetComponent(sessionEntity, session);
        }
    }

    private bool TryGetChoiceState(EcsWorld world, out Entity sessionEntity, out LevelUpChoiceState state)
    {
        sessionEntity = EnsureSessionEntity(world) ?? Entity.None;
        if (sessionEntity == Entity.None)
        {
            state = default;
            return false;
        }

        if (!world.TryGetComponent(sessionEntity, out state))
        {
            return false;
        }

        return true;
    }

    private Entity? EnsureSessionEntity(EcsWorld world)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            return _sessionEntity;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        return _sessionEntity;
    }

    private void ClearState(EcsWorld world)
    {
        if (!TryGetChoiceState(world, out var sessionEntity, out _))
        {
            return;
        }

        world.RemoveComponent<LevelUpChoiceState>(sessionEntity);
        world.RemoveComponent<LevelUpChoiceHistory>(sessionEntity);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        // Top
        spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }

    private static string WrapText(SpriteFont font, string text, float maxLineWidth)
    {
        var words = text.Split(' ');
        var sb = new StringBuilder();
        var line = string.Empty;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (font.MeasureString(testLine).X <= maxLineWidth)
            {
                line = testLine;
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(line);
            line = word;
        }

        if (!string.IsNullOrEmpty(line))
        {
            if (sb.Length > 0)
            {
                sb.Append('\n');
            }
            sb.Append(line);
        }

        return sb.ToString();
    }
}

