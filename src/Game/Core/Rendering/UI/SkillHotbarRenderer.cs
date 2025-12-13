using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Skills;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Rendering.UI;

/// <summary>
/// Renders the skill hotbar UI showing equipped skills, cooldown timers, and hotkey bindings using Myra UI.
/// </summary>
internal sealed class SkillHotbarRenderer : IUiDrawSystem, ILoadContentSystem
{
    private const int SlotCount = 5;
    
    private Desktop _desktop = null!;
    private Grid _mainContainer = null!;
    private HorizontalProgressBar _castBar = null!;
    private readonly List<Image> _slotIcons = new();
    private readonly List<Label> _slotHotkeys = new();
    private readonly List<Panel> _cooldownOverlays = new(); // Semi-transparent panels for CD

    private readonly SkillRegistry _skillRegistry;
    private Dictionary<SkillId, TextureRegion> _skillRegions = null!;
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
        UiFonts.Load(content);
        _desktop = new Desktop();
        
        // Load textures and create regions for Myra
        _skillRegions = new Dictionary<SkillId, TextureRegion>();
        LoadSkillIcon(content, SkillId.Firebolt, "Sprites/icons/abilities/Firebolt");
        LoadSkillIcon(content, SkillId.Fireball, "Sprites/icons/abilities/Fireball");
        LoadSkillIcon(content, SkillId.FlameWave, "Sprites/icons/abilities/FlameWave");
        LoadSkillIcon(content, SkillId.ArcaneMissile, "Sprites/icons/abilities/ArcaneMissile");
        LoadSkillIcon(content, SkillId.ArcaneBurst, "Sprites/icons/abilities/ArcaneBurst");
        LoadSkillIcon(content, SkillId.ArcaneBarrage, "Sprites/icons/abilities/ArcaneBarrage");
        LoadSkillIcon(content, SkillId.FrostBolt, "Sprites/icons/abilities/FrostBolt");
        LoadSkillIcon(content, SkillId.FrostNova, "Sprites/icons/abilities/FrostNova");
        LoadSkillIcon(content, SkillId.Blizzard, "Sprites/icons/abilities/Blizzard");

        // Build the UI Hierarchy
        BuildUi();
    }

    private void BuildUi()
    {
        _mainContainer = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 20) // Bottom margin
        };

        // Row 0: Cast Bar
        // Row 1: Hotbar Slots
        _mainContainer.RowsProportions.Add(new Proportion(ProportionType.Auto));
        _mainContainer.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // 1. Cast Bar
        _castBar = new HorizontalProgressBar
        {
            Width = 200,
            Height = 12,
            Visible = false,
            GridRow = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _mainContainer.Widgets.Add(_castBar);

        // 2. Hotbar Grid
        var hotbarGrid = new Grid
        {
            GridRow = 1,
            ColumnSpacing = 8
        };

        for (int i = 0; i < SlotCount; i++)
        {
            hotbarGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));
            
            // Slot Container (Panel)
            var slotPanel = new Panel
            {
                Width = 48,
                Height = 48,
                GridColumn = i,
                Background = new SolidBrush(new Color(40, 40, 40, 200)),
                Border = new SolidBrush(Color.Gray),
                BorderThickness = new Thickness(2)
            };

            // Icon
            var icon = new Image
            {
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _slotIcons.Add(icon);
            slotPanel.Widgets.Add(icon);

            // Cooldown Overlay (Darkens the slot)
            var cdOverlay = new Panel
            {
                Background = new SolidBrush(new Color(0, 0, 0, 180)),
                Height = 0, // Height controlled by CD %
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _cooldownOverlays.Add(cdOverlay);
            slotPanel.Widgets.Add(cdOverlay);

            // Hotkey Label
            var label = new Label
            {
                Text = i == 0 ? "LMB" : i.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextColor = Color.White,
                Font = UiFonts.Body // Or your custom font
            };
            _slotHotkeys.Add(label);
            slotPanel.Widgets.Add(label);

            hotbarGrid.Widgets.Add(slotPanel);
        }

        _mainContainer.Widgets.Add(hotbarGrid);
        _desktop.Root = _mainContainer;
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        UpdateWidgets(world);

        // Important: Break the current batch to let Myra handle its own rendering
        context.SpriteBatch.End();
        
        _desktop.Render();
        
        // Restore the batch for subsequent systems
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: context.Camera.Transform);
    }

    private void UpdateWidgets(EcsWorld world)
    {
        // 1. Find Player
        if (_playerEntity is null || !world.IsAlive(_playerEntity.Value))
        {
            world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => _playerEntity = entity);
        }
        if (!_playerEntity.HasValue) return;

        var player = _playerEntity.Value;

        // 2. Update Cast Bar
        if (world.TryGetComponent(player, out SkillCasting casting))
        {
            _castBar.Visible = true;
            _castBar.Value = casting.CastProgress * _castBar.Maximum;
            // You can change _castBar.Filler color based on element here
        }
        else
        {
            _castBar.Visible = false;
        }

        // 3. Update Slots
        if (world.TryGetComponent(player, out EquippedSkills equipped) &&
            world.TryGetComponent(player, out SkillCooldowns cooldowns))
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var skillId = equipped.GetSkill(i);
                
                // Update Icon
                if (skillId != SkillId.None && _skillRegions.TryGetValue(skillId, out var region))
                {
                    _slotIcons[i].Renderable = region;
                    _slotIcons[i].Visible = true;
                }
                else
                {
                    _slotIcons[i].Visible = false;
                }

                // Update Cooldown
                var cdRemaining = cooldowns.GetCooldown(skillId);
                var skillDef = _skillRegistry.GetSkill(skillId);
                
                if (cdRemaining > 0 && skillDef != null)
                {
                    float pct = cdRemaining / skillDef.BaseCooldown;
                    _cooldownOverlays[i].Height = (int)(48 * pct); // Animate height
                }
                else
                {
                    _cooldownOverlays[i].Height = 0;
                }
            }
        }
    }

    private void LoadSkillIcon(ContentManager content, SkillId skillId, string path)
    {
        try
        {
            var texture = content.Load<Texture2D>(path);
            _skillRegions[skillId] = new TextureRegion(texture);
        }
        catch (ContentLoadException)
        {
            Console.WriteLine($"[SkillHotbarRenderer] Warning: Could not load icon for {skillId} at {path}");
        }
    }

}
