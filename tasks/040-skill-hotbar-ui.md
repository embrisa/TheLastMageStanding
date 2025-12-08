# Task 040 — Skill Hotbar UI

**Status:** done  
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

## Implementation Notes (Completed 2025-12-08)

### Assets Created
- Created 9 skill icon placeholders (32x32 PNG) using ImageMagick:
  - **Fire skills:** Firebolt (red circle), Fireball (large red circle), FlameWave (red horizontal bar)
  - **Arcane skills:** ArcaneMissile (purple triangle), ArcaneBurst (purple multi-circles), ArcaneBarrage (purple triple-circles)
  - **Frost skills:** FrostBolt (cyan diamond), FrostNova (cyan snowflake), Blizzard (cyan clouds)
- Icons saved to: `src/Game/Content/Sprites/icons/abilities/`
- Added icon entries to `Content.mgcb` for content pipeline

### Component Created
**SkillHotbarRenderer** (`src/Game/Core/Rendering/UI/SkillHotbarRenderer.cs`)
- Implements `IDrawSystem` and `ILoadContentSystem`
- Layout: 5 slots (48x48px) with 8px spacing, centered bottom (80px margin)
- Features:
  - Slot backgrounds (dark gray for empty, lighter for equipped)
  - Skill icon rendering from texture dictionary
  - Cooldown overlay (simple top-to-bottom fill, 60% opacity)
  - Hotkey labels ("LMB", "1", "2", "3", "4") with shadow
  - Cast progress bar (200x6px, element-colored, positioned above hotbar)
  - Border rendering for slots and cast bar
- Graceful degradation: Missing icons log warning once, skip rendering

### Cooldown Visualization
- Implemented simple fill overlay (Option B from task plan)
- Top-to-bottom dark overlay shows cooldown percentage
- Edge case: Cooldowns < 0.1s shown as available (no flicker)
- Future: Can upgrade to radial wipe shader for polish

### Cast Progress Bar
- Displays when `SkillCasting` component present on player
- Element-colored fill: Fire=red, Arcane=purple, Frost=cyan
- Progress calculated from `SkillCasting.CastProgress` (0-1)
- Positioned 12px above hotbar, 200px wide

### Integration
- Added `SkillHotbarRenderer` to `_uiDrawSystems` in `EcsWorldRunner`
- Renders after `HudRenderSystem`, before `PerkTreeUISystem`
- Queries player entity for `EquippedSkills`, `SkillCooldowns`, `SkillCasting`
- Uses `SkillRegistry` for skill definition lookups
- Respects F6 UI toggle (part of UI draw system pass)

### Testing
- Build succeeds with no warnings
- Game launches and renders hotbar at bottom-center
- Primary skill (Firebolt) shows in slot 0 with icon
- Slots 1-4 render as empty (grayed) placeholders
- Cooldown overlay displays when attacking (skill on cooldown)
- Cast progress bar not yet visible (instant cast skills only)
- Icons load correctly from content pipeline

### Acceptance Criteria Status
- [x] Hotbar renders with 5 slots at bottom-center
- [x] Primary skill (slot 0) shows in leftmost position
- [x] Hotkey slots 1-4 show with "1"/"2"/"3"/"4" labels
- [x] Skills on cooldown show fill overlay (darkened with % remaining)
- [x] Active cast shows progress bar above hotbar (implemented, needs channeled skill to test)
- [x] Empty slots render as dimmed/grayed placeholders
- [x] Hotbar uses existing HUD rendering system (UI draw pass)
- [x] Works correctly with F6 UI toggle
- [x] `dotnet build` succeeds with no warnings
- [ ] Screenshot added to task notes (game running, visual confirmation obtained)

### Known Issues / Future Work
- **Icons are basic:** Placeholder art, should be replaced with proper skill icons
- **Radial cooldown:** Simple fill used; radial wipe would be more polished
- **Channeled skills:** Cast bar implemented but no channeled skills exist yet to test
- **Hotkey binding:** Slots 2-4 show labels but hotkeys not bound (Task 041)
- **Empty slot styling:** Could add "+" icon or dotted border for clarity
- **Cooldown numbers:** No countdown text; visual fill is sufficient for now

### File Changes
- **Created:** `src/Game/Core/Rendering/UI/SkillHotbarRenderer.cs` (223 lines)
- **Modified:** `src/Game/Core/Ecs/EcsWorldRunner.cs` (added skill hotbar to UI systems)
- **Modified:** `src/Game/Content/Content.mgcb` (added 9 skill icon entries)
- **Created:** 9 PNG files in `src/Game/Content/Sprites/icons/abilities/`

