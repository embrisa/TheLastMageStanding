# Task: 037 - Meta progression foundations
- Status: backlog

## Summary
Establish the foundational data models and persistence layer for meta progression. Create player profile with meta level/XP/gold tracking, run session tracking, and basic persistence service. This is the foundation for future meta hub, equipment, and shop systems.

## Goals
- Create `PlayerProfile` data model with meta level, XP, gold, equipment inventory, and talent unlocks.
- Create `RunSession` model to track stats during gameplay.
- Implement `PlayerProfileService` for JSON persistence with versioning and corruption handling.
- Implement `RunHistoryService` for storing and querying past run data.
- Create `MetaProgressionCalculator` for XP/level formulas.
- Integrate profile loading at game start and saving at game over.
- Add basic tests for persistence and calculations.

## Non Goals
- Meta hub UI (defer to Task 045)
- Shop/equipment purchase UI (defer to Task 046)
- Talent tree integration (defer to Task 047)
- In-run inventory UI (defer to Task 048)
- Run history/stats display UI (defer to Task 049)
- Complex economy tuning; keep values conservative for now.
- Cloud sync or account systems.
- Multiplayer considerations.

## Acceptance criteria
- [ ] `PlayerProfile` model exists with meta level, XP, gold, equipment inventory, and talent unlocks.
- [ ] `RunSession` model tracks wave, kills, damage, gold, duration, and equipment found.
- [ ] `PlayerProfileService` persists profile to JSON with atomic writes and backup system.
- [ ] `RunHistoryService` stores last 50 runs and supports querying.
- [ ] `MetaProgressionCalculator` calculates meta XP from run performance and determines level from XP.
- [ ] Profile is loaded at game start and saved at game over.
- [ ] Run stats are captured during gameplay and converted to meta XP on game over.
- [ ] Tests cover profile persistence, XP calculations, corruption handling, and versioning.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests pass (`dotnet test`)
- Docs updated (data models, persistence format, XP formulas)
- Handoff notes added (if handing off)
- Follow-up tasks created for UI and integration work

## Plan

### Scope for Task 037 (Foundations Only)
This task focuses ONLY on data models, persistence, and calculation logic. No UI work.

**What's included:**
- Data models: `PlayerProfile`, `RunSession`, `EquipmentItem`
- Services: `PlayerProfileService`, `RunHistoryService`, `MetaProgressionCalculator`
- Persistence: JSON file I/O with versioning, backups, corruption handling
- Integration: Load profile at game start, save at game over, capture run stats
- Tests: Persistence, calculations, versioning

**What's deferred to future tasks:**
- Task 045: Meta Hub UI & Scene
- Task 046: Shop & Equipment Purchase UI
- Task 047: Talent Tree Integration & Application
- Task 048: In-Run Inventory & Equipment Swapping UI
- Task 049: Run History & Stats Display UI

### Implementation Steps (3-5 days)

#### Step 1: Data Models (1 day)
- Create `Core/MetaProgression/PlayerProfile.cs`:
  - Meta level & XP
  - Total gold
  - Equipment inventory (list of `EquipmentItem`)
  - Equipped loadout (weapon, armor, accessories)
  - Unlocked talent nodes (list of IDs)
  - Run statistics (best wave, total runs, total kills, etc.)
  - Profile creation timestamp, last played timestamp
  - Schema version field
- Create `Core/MetaProgression/RunSession.cs`:
  - Run start/end time, duration
  - Wave reached, kills, damage dealt/taken, gold collected
  - Meta XP earned (calculated at end)
  - Equipment found during run (list of `EquipmentItem`)
  - Skills used, cause of death
- Create `Core/MetaProgression/EquipmentItem.cs`:
  - Item ID, name, type (weapon/armor/accessory)
  - Rarity, stats (damage, armor, etc.)
  - Icon/sprite reference
- Add tests for model serialization/deserialization

#### Step 2: Persistence Layer (1-2 days)
- Create `Core/MetaProgression/PlayerProfileService.cs`:
  - `LoadProfile()` — load from JSON, handle missing/corrupted files gracefully
  - `SaveProfile(PlayerProfile)` — atomic write with temp file + rename
  - `BackupProfile()` — keep last 3 backups
  - Platform-specific user data directory detection (Windows/macOS/Linux)
  - Profile versioning and migration support
- Create `Core/MetaProgression/RunHistoryService.cs`:
  - `SaveRun(RunSession)` — append to history file
  - `GetRecentRuns(int count)` — query last N runs
  - `GetBestRuns()` — query personal records
  - Maintain last 50 runs (rolling window)
- Add file I/O abstraction interface for testability
- Add tests for persistence:
  - Load/save profile successfully
  - Handle corrupted JSON gracefully (create default profile)
  - Verify atomic writes (no partial files)
  - Test backup system
  - Test versioning/migration

#### Step 3: Meta Progression Calculator (1 day)
- Create `Core/MetaProgression/MetaProgressionCalculator.cs`:
  - `CalculateMetaXP(RunSession)` — compute XP from run performance
    - Base XP from wave reached (exponential curve)
    - Bonus XP from kills, gold, damage dealt
    - Time multiplier (reward efficient runs)
  - `GetLevelFromXP(int totalXP)` — determine meta level
  - `GetXPForLevel(int level)` — XP threshold for given level
  - `GetXPToNextLevel(int currentXP)` — XP remaining to next level
- Make formulas data-driven (constants at top or in config)
- Initial formulas:
  ```
  base_xp = wave_reached^1.5 * 100
  kill_bonus = total_kills * 5
  gold_bonus = gold_collected * 2
  damage_bonus = damage_dealt / 1000
  time_multiplier = max(0, 1 - (run_duration_minutes / 60))
  meta_xp = (base_xp + kill_bonus + gold_bonus + damage_bonus) * (1 + time_multiplier * 0.5)
  
  xp_for_level_n = 1000 * (n^1.8)
  ```
- Add tests for XP calculations:
  - Various run scenarios (short/long, high/low kills)
  - Level thresholds
  - Edge cases (wave 0, negative values)

#### Step 4: Integration with Game Loop (1 day)
- Hook profile loading at game initialization:
  - Load profile in `Game1.Initialize()` or early startup
  - Store profile in accessible service/singleton
  - Create default profile if none exists
- Hook run session tracking:
  - Start new `RunSession` when gameplay begins (via `RunStartedEvent`)
  - Capture stats incrementally during gameplay (hook into existing events from Task 019)
  - Finalize `RunSession` on game over
- Hook profile saving at game over:
  - Calculate meta XP from `RunSession`
  - Update `PlayerProfile` (add XP, add gold, update stats)
  - Save new equipment found to profile inventory
  - Save profile to disk
  - Save run to history
- Add event integration:
  - `RunStartedEvent` → initialize run session
  - `RunEndedEvent` → finalize run, update profile, save
  - `GoldGainedEvent` → track gold in run session
  - `EquipmentDroppedEvent` → add to run session equipment list
- No UI yet — log profile changes to console for verification

#### Step 5: Testing & Documentation (1 day)
- Write unit tests:
  - Profile service: load/save, corruption handling, backups
  - Run history service: save/query runs
  - Meta progression calculator: XP formulas, level calculations
- Write integration test:
  - Full cycle: load profile → simulate run → save profile → reload → verify persistence
- Document:
  - Data model schemas (JSON format examples)
  - Persistence file locations and naming
  - XP formulas and tuning parameters
  - Extension points for future tasks
- Create design doc: `docs/design/037-meta-progression-foundations.md`
- Update `game-design-document.md` with meta progression overview
- Run `dotnet build` and fix any errors

### Estimated Timeline
- **Total: 3-5 days** for foundations only
- Future UI and integration work deferred to Tasks 045-049

### Future Tasks to Create

**Task 045: Meta Hub UI & Scene**
- Create meta hub game state and UI layout
- Top bar with meta level/XP/gold display
- Navigation to shop, talents, run history, start run
- Estimated: 3-4 days

**Task 046: Shop & Equipment Purchase UI**
- Shop UI for browsing/purchasing equipment
- Equipment UI for viewing inventory and equipping loadout
- Gold transaction handling
- Estimated: 3-4 days

**Task 047: Talent Tree Integration & Application**
- Connect to Task 031 talent tree
- Apply unlocked talents at run start
- Talent unlock/reset UI in meta hub
- Estimated: 2-3 days

**Task 048: In-Run Inventory & Equipment Swapping**
- In-game inventory UI accessible via hotkey
- Equipment swapping during runs
- Integration with equipment drops from Task 030
- Estimated: 2-3 days

**Task 049: Run History & Stats Display UI**
- Run history browser
- Aggregate stats and personal records
- Graph/trend visualization
- Estimated: 2-3 days

### Context & Design Overview (for reference)

**Two-tier progression system:**
1. **In-game level** — earned during runs via XP orbs (Task 022), resets each run, unlocks skill upgrades
2. **Meta level** — persistent account XP from runs, unlocks talent-perk tree (Task 031), permanent
3. **Gold currency** — collected in-game, persists across runs, spent in meta hub for equipment/unlocks
4. **Equipment inventory** — permanent collection of weapons/armor, never resets; can swap equipment during runs via inventory menu

**Equipment acquisition:** Equipment drops from enemies during runs (Task 030) AND can be purchased in meta hub shop (Task 046). All dropped equipment is automatically added to permanent collection.

**Gold sources:** Enemy kills, loot drops, wave/run completion bonuses
**Gold sinks:** Equipment purchases, talent unlocks, cosmetics

This foundational task establishes the data layer; UI and gameplay integration come in Tasks 045-049.

### Technical Decisions
### Architecture Notes (for reference)

#### New Systems (Task 037 only)
- `Core/MetaProgression/` namespace:
  - `PlayerProfile.cs` — data model
  - `RunSession.cs` — run tracking model
  - `PlayerProfileService.cs` — persistence
  - `RunHistoryService.cs` — history tracking
  - `MetaProgressionCalculator.cs` — XP/level formulas
  - `EquipmentItem.cs` — equipment data model (stats, rarity, type)

#### Future Systems (Tasks 045-049)
- `MetaProgressionApplicator.cs` — apply bonuses/equipment at run start (Task 047)
- `MetaShopService.cs` — shop transactions (Task 046)
- `EquipmentInventoryService.cs` — equipment collection management (Task 046)
- `TalentUnlockService.cs` — talent unlock logic (Task 047)
- UI components in Tasks 045-049

#### Event Integration
- Reuse event bus (Task 013) for:
  - `RunStartedEvent` — initialize run session
  - `RunEndedEvent` — finalize and save run session, persist new equipment drops
  - `MetaXpGainedEvent` — trigger level-up notifications
  - `GoldGainedEvent` — update profile gold
  - `EquipmentDroppedEvent` — add dropped equipment to permanent collection (Task 030 integration)

#### Save Timing
- Auto-save profile on:
  - Run end (game over)
  - Game quit (via application exit event)
- Future: Talent unlock/reset, shop purchase (Tasks 046-047)
- Never save during gameplay (only at safe points)

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; meta unlocks should target that class for now.
- Persistence must be robust to schema changes; include versioning/defaults.
- Keep XP/level formulas data-driven (constants at top of calculator) for easy tuning.
- Start with conservative gold economy values; iterate based on playtest feedback.
- Equipment data model must align with Task 029 stat model and Task 030 loot system.

