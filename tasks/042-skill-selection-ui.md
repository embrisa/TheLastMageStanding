# Task 042 — Skill Selection & Equipping UI

**Status:** SUPERSEDED by Task 055  
**Priority:** ~~Medium~~ → **See Task 055**  
**Estimated effort:** ~~3-4 hours~~ → **See Task 055**  
**Dependencies:** ~~Task 040 (Skill hotbar UI), Task 041 (Hotkey input)~~ → **REPLACED by Task 055**

**NOTE:** This task is fully replaced by Task 055 (Skill selection hub UI), which implements skill browsing/equipping in the hub scene per the new game vision.

## Summary

Create a skill selection overlay where players can browse available skills and equip them to hotbar slots. Currently, skills are hardcoded at player spawn; this adds player agency and discovery.

## Rationale

The skill system has 9 mage skills but players can't choose which ones to equip. A selection UI enables build customization, experimentation, and unlocking new skills as the player progresses (future: tie to level/XP).

## Goals

- Create skill selection overlay (toggle with `K` key for "skills")
- Show grid of available skills grouped by element (Fire/Arcane/Frost)
- Display skill details on hover/select: name, element, description, damage, cooldown, cast time
- Click skill then click hotbar slot to equip
- Persist equipped skills across sessions (JSON save/load)
- Visual feedback for currently equipped skills
- Support keyboard navigation (arrow keys + Enter)

## Non-Goals

- Skill unlock progression (all skills available initially; gating is future task)
- Skill trees/prerequisites (future)
- Drag-and-drop equipping (click-to-equip is sufficient)
- In-combat skill swapping (overlay only opens when Playing or Paused)
- Skill comparison tooltips (future polish)
- Gamepad support (keyboard + mouse only)

## Acceptance Criteria

1. `K` key toggles skill selection overlay (like `P` for perks, `I` for inventory)
2. Overlay shows 9 mage skills in 3×3 grid (3 per element)
3. Skills grouped/labeled by element with color coding
4. Clicking skill shows detailed tooltip panel
5. Selected skill + hotbar slot click equips skill to that slot
6. Currently equipped skills show "Equipped: Slot X" badge
7. Empty slot click with selected skill equips to that slot
8. Equipped slot click with selected skill swaps/replaces
9. Escape closes overlay without changes (unless explicitly confirmed)
10. Equipped skills persist to JSON, load on game start

## Plan

### 1. Create SkillSelectionOverlay component
**File:** `src/Game/Core/UI/SkillSelectionOverlay.cs`

```csharp
public class SkillSelectionOverlay
{
    public bool IsOpen { get; set; }
    
    private SkillId? _selectedSkill;
    private int _gridCursorX;
    private int _gridCursorY;
    
    // Grid layout: 3 columns (Fire, Arcane, Frost) × 3 rows
    private readonly SkillId[][] _skillGrid = new[]
    {
        new[] { SkillId.Firebolt, SkillId.Arcanemissile, SkillId.FrostBolt },
        new[] { SkillId.Fireball, SkillId.ArcaneBurst, SkillId.FrostNova },
        new[] { SkillId.FlameWave, SkillId.ArcaneBarrage, SkillId.Blizzard }
    };
}
```

### 2. Create SkillSelectionRenderSystem
**File:** `src/Game/Core/Rendering/UI/SkillSelectionRenderSystem.cs`

Render order:
1. Dimmed background overlay (semi-transparent black)
2. Main panel (centered, 600×500px)
3. Title: "Skill Selection"
4. Element headers: "Fire" (red), "Arcane" (purple), "Frost" (cyan)
5. Skill grid (3×3, 64×64px icons, 16px spacing)
6. Detail panel (right side): selected skill info
7. Hotbar preview (bottom): shows current equipped skills
8. Instructions: "Click skill, then click hotbar slot to equip. ESC to close."

### 3. Create SkillSelectionInputSystem
**File:** `src/Game/Core/UI/SkillSelectionInputSystem.cs`

Input handling:
- **K key:** Toggle overlay open/closed
- **Arrow keys:** Navigate grid cursor
- **Enter:** Select skill at cursor (keyboard mode)
- **Mouse click:** Select skill at mouse position
- **Hotbar click:** Equip selected skill to clicked slot
- **Escape:** Close overlay

State management:
- Only process input when overlay is open
- Block game input when overlay is open (similar to pause menu)
- Track selected skill and target slot

### 4. Skill detail panel content
Display for selected skill:
```
┌─────────────────────────────┐
│ Firebolt                    │ (name in element color)
│ Fire • Projectile           │ (element + delivery type)
├─────────────────────────────┤
│ A fast fire projectile that │ (description)
│ deals moderate damage.       │
│                              │
│ Damage: 1.0× power          │ (stats)
│ Cooldown: 0.5s              │
│ Cast Time: Instant          │
│ Projectile Speed: 500       │
│ Range: 800                  │
│                              │
│ Equipped: Primary           │ (if equipped)
└─────────────────────────────┘
```

### 5. Persistence integration
**File:** `src/Game/Core/Skills/EquippedSkillsPersistence.cs`

```csharp
public class EquippedSkillsPersistence
{
    private const string SavePath = "Data/EquippedSkills.json";
    
    public void Save(EquippedSkills skills) { /* ... */ }
    public EquippedSkills? Load() { /* ... */ }
}
```

Auto-save triggers:
- On skill equip/swap
- On game exit (via `Game1.OnExiting`)
- Every 30s auto-save tick (like perks/equipment)

Load trigger:
- On player spawn in `PlayerEntityFactory`

### 6. Layout calculations
```
Virtual resolution: 960×540
Main panel: 600×500, centered at (180, 20)
Skill grid: 3 columns × 3 rows
Icon size: 64×64
Spacing: 16px between icons
Grid origin: (200, 80)

Detail panel: 240×400, at (620, 80)
Hotbar preview: 272×64, centered at (344, 480)
```

### 7. Visual states
- **Default:** Gray border, normal opacity
- **Hover:** Yellow border, slight glow
- **Selected:** Gold border, highlighted
- **Equipped:** Green checkmark badge, "Equipped: Slot X" text
- **Empty slot:** Dashed border, dimmed

## Technical Notes

### Persistence Format
```json
{
  "version": 1,
  "primary": "Firebolt",
  "hotkey1": "Fireball",
  "hotkey2": "ArcaneBarrage",
  "hotkey3": "FrostBolt",
  "hotkey4": null
}
```

Serialize `SkillId` as string enum for readability and version tolerance.

### Input Flow
```
1. Player presses K → open overlay
2. Player clicks Fireball → _selectedSkill = Fireball
3. Player clicks hotkey slot 2 → equip Fireball to slot 2
4. System publishes SkillEquippedEvent (for SFX/feedback)
5. System triggers auto-save
6. Hotbar UI updates to show Fireball in slot 2
```

### Conflict Handling
- **Equipping skill already equipped:** Move from old slot to new slot (swap)
- **Equipping to occupied slot:** Replace existing skill (no confirmation for now)
- **Primary slot:** Can never be empty (if cleared, defaults to Firebolt)

## Definition of Done

- [ ] `K` key toggles skill selection overlay
- [ ] Overlay shows 9 skills in 3×3 grid with element grouping
- [ ] Clicking skill selects it and shows detail panel
- [ ] Clicking hotbar slot with selected skill equips it
- [ ] Equipped skills show badge/indicator
- [ ] Escape closes overlay
- [ ] Equipped skills persist to JSON
- [ ] Equipped skills load on game start
- [ ] Primary slot can't be left empty (defaults to Firebolt)
- [ ] `dotnet build` succeeds
- [ ] Manual test: equip all 9 skills, restart game, verify persistence
- [ ] Manual test: swap skills between slots, verify state updates
- [ ] Screenshot added to task notes showing overlay

## Risks & Unknowns

- **UI complexity:** Most complex overlay so far (more elements than pause/perk UIs)
- **Mouse hit detection:** Need pixel-perfect bounds checking for skill icons and hotbar slots
- **Persistence bugs:** Careful handling of null/empty slots and invalid skill IDs
- **Performance:** 9 skill icons + details = more draw calls; should be negligible but profile if needed

## Future Enhancements

- Drag-and-drop equipping (more intuitive than click-click)
- Skill comparison tooltips (show two skills side-by-side)
- Unlock animations when new skills become available
- Skill filtering/search (not needed for 9 skills, but scales for more)
- Preset loadouts (save/load multiple skill bar configurations)
- In-combat skill swapping (with cooldown penalty)
- Visual preview of skill effects (mini cast demo)
- Color-blind accessibility mode (patterns/shapes instead of colors)

## Notes

This task bridges skill system (backend) and player experience (frontend). Focus on clarity and usability—players should instantly understand how to equip skills without documentation.

**Design inspiration:** Diablo 2 skill selection (tree view) vs Diablo 3 (grid view). Grid is simpler and fits 9 skills well.

**Accessibility:** Ensure keyboard-only navigation works for all functionality (not just mouse-driven).
