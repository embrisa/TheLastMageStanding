# Task: 022 - XP orbs and level-ups
- Status: completed
- ⚠️ **NEEDS UPDATE**: See design clarification below

## ⚠️ Design Clarification (Dec 8, 2025)

**New Vision**: Level-ups should present a **CHOICE** between stat boost OR skill modifier, not automatic fixed bonuses.

**Current Implementation**: Grants fixed stat bonuses (+2 damage, +5 speed, +10 HP) automatically.

**Required Changes**:
1. Add level-up choice UI showing 2 options.
2. Pause game when level-up occurs until player makes choice.
3. Stat boost option: Choose from multiple stat types (+HP, +Damage, +Speed, +Armor, etc.).
4. Skill modifier option: Choose modifier for an equipped skill (+Damage%, -Cooldown%, +AoE%, etc.).
5. Choices are temporary (reset on stage restart).

**See**: `/docs/DESIGN_CLARIFICATION.md` for full context.

---

## Summary
Runs end without progression feedback. Add lightweight XP orbs on enemy death, collection mechanics, and a basic level-up flow that boosts player stats, giving players a sense of growth across a run.

## Goals
- Spawn XP orbs on enemy death events; make them collectible via proximity with optional magnet pull.
- Track XP/level data on the player; define XP curve and per-level stat bonuses (e.g., damage, move speed, max HP).
- Render an XP bar in the HUD that scales with virtual resolution.
- Show a simple level-up notification; apply bonuses immediately without a full choice UI.

## Non Goals
- Talent trees, reroll UI, or meta-progression persistence.
- New art/audio assets beyond simple orb sprites/primitive and UI bar.
- Complex pickup physics; keep movement simple and performant.

## Acceptance criteria
- [x] Enemy death emits XP orbs that appear in the world, can be collected, and despawn on pickup.
- [x] Player accrues XP and levels up according to a defined curve; levels grant immediate stat bonuses from config.
- [x] HUD shows current XP progress and updates in real time; level-up notification is visible and non-blocking.
- [x] XP and levels reset cleanly on run restart; no carry-over between runs.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define XP/level components/config; listen to enemy death events to spawn XP orbs with pickup/lifetime behavior.
- Step 2: Implement XP collection and leveling logic on the player; apply stat bonuses on level-up.
- Step 3: Add HUD XP bar and level-up notification; ensure scaling with virtual resolution.
- Step 4: Verify restart resets XP/level state and orbs; run `dotnet build`.

## Notes / Risks / Blockers
- Avoid spawning excessive orbs; consider clamping per-enemy XP or merging close orbs.
- Ensure pickups respect faction (player-only) and do not conflict with other interactions.
- Keep HUD bar performant; reuse existing fonts/atlas to avoid content pipeline churn.

## Implementation Notes (Completed)

### Components Added
- `ProgressionComponents.cs`: `XpOrb` (marks collectible orbs with XP value), `PlayerXp` (tracks currentXp, level, xpToNextLevel)

### Events Added
- `PlayerProgressionEvents.cs`: `XpCollectedEvent`, `PlayerLeveledUpEvent`

### Config Added
- `ProgressionConfig.cs`: Configurable XP curve (base 10, growth 1.5x), stat bonuses per level (damage +2, speed +5, health +10), orb behavior (lifetime 10s, collection radius 40px, magnet radius 120px)

### Systems Added
1. **XpOrbSpawnSystem**: Subscribes to `EnemyDiedEvent`, spawns XP orb entities at enemy death positions with Position, Velocity, XpOrb, and Lifetime components
2. **XpCollectionSystem**: Checks proximity between player and orbs, applies magnet pull via Lerp for smooth movement, destroys orbs on contact, adds XP to player, detects level-up thresholds, publishes level-up events
3. **LevelUpSystem**: Subscribes to `PlayerLeveledUpEvent`, applies stat bonuses to MoveSpeed, AttackStats.Damage, and Health.Max (maintaining health ratio), creates level-up notification using WaveNotification
4. **XpOrbRenderSystem**: Renders XP orbs in world space using `Sprites/objects/ExperienceShard` texture

### HUD Enhancements
- Extended `HudRenderSystem` to display level and XP progress bar below kills counter
- XP bar shows current/required XP with gold gradient fill
- Level displayed as "Level X"

### Factory & Restart Changes
- `PlayerEntityFactory` now takes `ProgressionConfig` and initializes `PlayerXp` component on player creation (level 1, 0 XP)
- `GameSessionSystem.RestartSession` clears all XP orbs, resets player stats to base values (220 speed, 20 damage, 100 health), resets XP to level 1

### System Registration
- All new systems registered in `EcsWorldRunner` update/draw pipeline
- XP orb spawn/collection/level-up systems run between damage numbers and cleanup
- XP orb render system added to world draw pass

### Technical Details
- Magnet pull uses `Vector2.Lerp` for smooth organic movement without physics complexity
- XP curve formula: `BaseXP * (GrowthFactor ^ (Level - 1))`
- Level-up can chain if collecting enough XP (while loop handles multiple levels)
- Orbs auto-despawn after 10 seconds via Lifetime component
- Build passes: `dotnet build` succeeded






