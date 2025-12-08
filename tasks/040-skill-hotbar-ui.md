# Task 040 — Skill Hotbar UI

**Status:** backlog  
**Priority:** High  
**Estimated effort:** 2-3 hours  
**Dependencies:** Task 039 (Mage skill system)

## Summary

Add a visual skill hotbar to the HUD showing equipped skills, cooldown timers, and hotkey bindings. Players need immediate visual feedback for skill availability and casting state.

## Rationale

The skill system is fully functional but invisible—players can't see what skills they have equipped, when cooldowns expire, or which hotkeys to press. A hotbar provides essential combat feedback and makes the skill system discoverable and usable.

## Goals

- Render skill hotbar with 5 slots (primary + hotkeys 1-4) at bottom-center of screen
- Show skill icons, hotkey labels, and cooldown overlays
- Display active cast progress bar when casting
- Highlight equipped vs empty slots
- Support keyboard navigation for future skill swapping UI

## Non-Goals

- Skill selection/equipping UI (future task)
- Skill tooltips on hover (future task)
- Drag-and-drop hotbar customization (future)
- Mouse-click skill activation (keyboard only for now)
- Cooldown number countdown (visual fill is sufficient)

## Acceptance Criteria

1. Hotbar renders at bottom-center with 5 skill slots
2. Primary skill (slot 0) shows in leftmost position
3. Hotkey slots 1-4 show with "1"/"2"/"3"/"4" labels
4. Skills on cooldown show radial/fill overlay (darkened with % remaining)
5. Active cast shows progress bar above hotbar
6. Empty slots render as dimmed/grayed placeholders
7. Hotbar uses existing HUD rendering system
8. Works correctly with F6 UI toggle

## Plan

### 1. Create SkillHotbarRenderer component
**File:** `src/Game/Core/Rendering/UI/SkillHotbarRenderer.cs`

```csharp
public class SkillHotbarRenderer : IDrawSystem
{
    private const int SlotCount = 5;
    private const int SlotSize = 48;
    private const int SlotSpacing = 8;
    private const int BottomMargin = 80;
    
    private SpriteBatch _spriteBatch;
    private Texture2D _slotTexture;
    private Texture2D _cooldownOverlay;
    private SpriteFont _hotkeyFont;
    
    // Draw slot backgrounds, skill icons, hotkey labels, cooldown overlays
}
```

### 2. Add skill icon assets
- Create placeholder skill icons (32x32) for 9 mage skills
- Add to Content.mgcb under `Sprites/UI/SkillIcons/`
- Naming: `Firebolt.png`, `Fireball.png`, etc.
- Color-code by element (red=Fire, purple=Arcane, cyan=Frost)

### 3. Implement cooldown visualization
- **Radial wipe:** Draw circular mask from 12 o'clock clockwise based on `remainingCD / totalCD`
- **Fill overlay:** Simple alpha-blended rectangle with gradient
- **Color scheme:** Dark gray overlay at 60% opacity
- **Edge case:** CD < 0.1s shows as available (no flicker)

### 4. Add cast progress bar
- Thin horizontal bar above hotbar (200px wide, 6px tall)
- Fill color: element color of casting skill
- Show when `SkillCasting` component present
- Progress = `SkillCasting.Progress` (0-1)

### 5. Integration points
- Read `EquippedSkills` component from player entity
- Read `SkillCooldowns` for remaining times
- Read `SkillCasting` for active cast state
- Lookup skill definitions from `SkillRegistry`
- Integrate with existing `HudRenderSystem` or create separate system

### 6. Layout calculation
```
Screen width: 960px (virtual resolution)
Hotbar width: (SlotSize × 5) + (SlotSpacing × 4) = 272px
X position: (960 - 272) / 2 = 344px (centered)
Y position: 540 - BottomMargin = 460px (bottom-anchored)
```

## Technical Notes

### Cooldown Overlay Options

**Option A: Radial wipe (preferred)**
- Use shader with angle calculation
- Smooth circular sweep effect
- Industry standard for ability cooldowns

**Option B: Simple fill**
- Top-to-bottom or bottom-to-top fill
- Easier implementation (no shader)
- Less visually polished

**Recommendation:** Start with Option B for quick implementation, upgrade to Option A if time permits.

### Icon Asset Strategy
- Placeholder icons can be solid circles with element colors
- Add rune/symbol overlays for differentiation
- Future: commission/purchase icon pack for production art

## Definition of Done

- [ ] Hotbar renders with 5 slots at bottom-center
- [ ] Skill icons display for equipped skills
- [ ] Hotkey labels ("1", "2", "3", "4") render on slots
- [ ] Cooldown overlays darken skills that aren't ready
- [ ] Cast progress bar shows during channeled skills
- [ ] Empty slots render as grayed placeholders
- [ ] Hotbar respects F6 UI toggle
- [ ] `dotnet build` succeeds with no warnings
- [ ] Visual test: equip different skills, trigger cooldowns, cast channeled skill
- [ ] Screenshot added to task notes showing hotbar in action

## Risks & Unknowns

- **Asset creation time:** May need placeholder icons initially
- **Cooldown shader complexity:** Radial wipe requires custom shader or stencil tricks
- **Performance:** Drawing 5+ sprites per frame should be negligible but verify with profiler
- **Input conflict:** Hotkeys 1-4 not bound yet (handled in Task 041)

## Future Enhancements

- Skill tooltips on hover (show damage, cooldown, description)
- Pulse/glow effect when skill comes off cooldown
- Number countdown overlay for long cooldowns (>5s)
- Resource cost display (mana/energy when implemented)
- Out-of-range indicator for targeted skills
- Keybinding customization

## Notes

Initial implementation should prioritize clarity and functionality over visual polish. Use MonoGame primitives and existing UI textures where possible. Polish pass can happen after core functionality is validated through playtesting.
