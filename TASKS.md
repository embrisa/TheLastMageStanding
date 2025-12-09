# Task Documentation & Workflow

This file indexes tasks. Each task lives in its own markdown file under `tasks/`, created from `TASK_TEMPLATE.md`. Keep entries concise and status-driven so agents can pick up quickly.

## Status labels
- `backlog` — not started, ready to be picked.
- `in_progress` — actively being worked on.
- `blocked` — needs input/unblocks.
- `in_review` — PR opened; awaiting review/testing.
- `done` — merged/completed; verify acceptance criteria.

## Required fields per task
- Title/ID
- Summary and rationale
- Goals / Non Goals
- Acceptance criteria (observable behaviors/outputs)
- Definition of done (tests/builds, docs, handoff)
- Plan
- Notes/risks/blockers

## Workflow
1. Copy `TASK_TEMPLATE.md` into `tasks/<id>-<slug>.md` and fill it out.
2. Add a bullet link under Active tasks.
3. Update status + notes daily.
4. On completion, verify acceptance criteria and testing notes, then mark `done`.
5. If handing off, leave next steps, open questions, and any gotchas in the task file.

Context: Tasks 031 and above assume the Mage is the first class with fire/arcane/frost skill & talent trees; future classes are out of scope for now.

## Active tasks


## Backlog - Priority Order (Post-040)
**Context:** Game vision updated to 4-act story-driven ARPG with hub-based configuration (see `/docs/DESIGN_CLARIFICATION.md`).

### IMMEDIATE (Do First)
- [053 - Remove mid-run configuration access](tasks/053-remove-mid-run-configuration.md) — Gate P/I/Shift+R behind hub; stop design conflicts

### HIGH PRIORITY (Foundation)
- [057 - Mouse-aimed skill targeting](tasks/057-mouse-aimed-skill-targeting.md) — Skills aim toward mouse cursor, not facing direction (do before 044)
- [041 - Skill hotkey input (keys 1-4)](tasks/041-skill-hotkey-input.md) — Bind 1-4 to cast skills (still valid, independent)
- [051 - Hub scene and scene management](tasks/051-hub-scene-and-scene-management.md) — Create hub scene, scene transitions (blocks 055, 056, 045-047)
- [050 - Level-up choice system](tasks/050-level-up-choice-system.md) — Replace auto-stat-boost with choice UI

### MEDIUM PRIORITY (Meta Progression)
- [054 - Meta progression system updates](tasks/054-meta-progression-updates.md) — Enforce level 60 cap, unlock tables (blocks 055)
- [055 - Skill selection hub UI](tasks/055-skill-selection-hub-ui.md) — Hub UI for skill equipping (**supersedes 042**)
- [056 - Equipment management hub UI](tasks/056-equipment-management-hub-ui.md) — Hub UI for equipment (**supersedes parts of 046**)
- [044 - Skill balance & feel pass](tasks/044-skill-balance-and-feel-pass.md) — Playtest and tune all skills
### HIGH PRIORITY (Hub Completion Arc)
- [061 - Debug NPC visibility](tasks/061-debug-npc-visibility.md) — **IN PROGRESS** Fix NPCs not appearing in hub
- [062 - Skill selection UI](tasks/062-skill-selection-ui.md) — Equip skills via `npc_ability_loadout` (blue NPC)
- [063 - Shop UI & equipment purchasing](tasks/063-shop-ui-equipment-purchasing.md) — Buy equipment via `npc_vendor` (gold NPC)
- [064 - Stats & run history UI](tasks/064-stats-run-history-ui.md) — View stats via `npc_archivist` (green NPC)
- [065 - Hub menu actions](tasks/065-hub-menu-actions.md) — Wire up Settings and Quit from ESC menu

### MEDIUM PRIORITY (Hub Polish Arc)
- [066 - NPC visual improvements](tasks/066-npc-visual-improvements.md) — Replace colored squares with sprites, animations
- [067 - Hub map environmental polish](tasks/067-hub-map-environmental-polish.md) — Decorations, lighting, music, ambience
- [068 - Hub tutorial & first-time UX](tasks/068-hub-tutorial-first-time-ux.md) — Guide new players through hub

### MEDIUM PRIORITY (Hub Features - Modified)
- [045 - Meta hub main menu](tasks/045-meta-hub-ui-and-scene.md) — **SUPERSEDED by Task 051** (hub is now playable world, not menu)
- [046 - Shop UI](tasks/046-shop-and-equipment-ui.md) — **SUPERSEDED by Task 055** (shop UI separate task now)
- [047 - Talent tree hub integration](tasks/047-talent-tree-integration.md) — **MOSTLY DONE** in Task 051 (P key works in hub)
- [049 - Run history UI](tasks/049-run-history-stats-ui.md) — **SUPERSEDED by Task 056** (stats UI via archivist NPC)

### MEDIUM PRIORITY (Campaign)
- [052 - Stage/act campaign system](tasks/052-stage-act-campaign-system.md) — 4 acts, stages, bosses, progression

### LOW PRIORITY / RECONSIDER
- ~~[042 - Skill selection UI](tasks/042-skill-selection-ui.md)~~ — **SUPERSEDED by Task 054**
- ~~[043 - Skill unlock progression](tasks/043-skill-unlock-progression.md)~~ — **SUPERSEDED by Task 054**
- [048 - In-run inventory UI](tasks/048-in-run-inventory-ui.md) — **CONFLICTS with new design; needs complete rewrite or removal**

## Done
- [051 - Hub scene & scene management](tasks/051-hub-scene-and-scene-management.md) — **REIMPLEMENTED 2024-12-09** Playable hub with NPC interactions
- [041 - Skill hotkey input (keys 1-4)](tasks/041-skill-hotkey-input.md)
- [040 - Skill hotbar UI](tasks/040-skill-hotbar-ui.md)
- [038 - Extended sound settings & volume controls](tasks/038-extended-sound-settings-and-volume-controls.md)
- [037 - Meta progression & run tracking (MVP)](tasks/037-meta-progression-and-run-tracking.md)
- [036 - Elite modifiers & mutators](tasks/036-elite-modifiers-and-mutators.md)
- [035 - Enemy AI variants & squad behaviors](tasks/035-enemy-ai-variants-and-squad-behaviors.md)
- [034 - Status effects & elemental interactions](tasks/034-status-effects-and-elemental-interactions.md)
- [033 - Dash/defense moves & i-frames](tasks/033-dash-defense-moves-and-i-frames.md)
- [039 - Mage skill system](tasks/039-skill-system.md)
- [032 - Elites/boss waves & rewards](tasks/032-elites-boss-waves-and-rewards.md)
- [031 - Talent/perk tree](tasks/031-talent-perk-tree.md)
- [030 - Loot and equipment foundations](tasks/030-loot-and-equipment-foundations.md)
- [029 - Unified stat and damage model](tasks/029-unified-stat-and-damage-model.md)
- [028 - Telegraphs, VFX/SFX, and hit-stop](tasks/028-telegraphs-vfx-sfx-hit-stop.md)
- [027 - Animation events & directional hitboxes](tasks/027-animation-events-and-directional-hitboxes.md)
- [021 - Ranged enemy and projectiles](tasks/021-ranged-enemy-and-projectiles.md)
- [024 - Static world collision](tasks/024-static-world-collision.md)
- [025 - Dynamic actor separation & knockback](tasks/025-dynamic-actor-separation-and-knockback.md)
- [026 - Collider-driven combat hits](tasks/026-collider-driven-combat-hits.md)
- [022 - XP orbs and level-ups](tasks/022-xp-orbs-and-level-ups.md)
- [023 - Collision system foundation](tasks/023-collision-system-foundation.md)
- [020 - Pause and settings overlay](tasks/020-pause-and-settings-overlay.md)
- [019 - Session stats and run summary](tasks/019-session-stats-and-run-summary.md)
- [018 - Game over & wave HUD](tasks/018-game-over-and-wave-hud.md)
- [017 - Directional TakeDamage animation](tasks/017-directional-takedamage-animation.md)
- [016 - Game State Events](tasks/016-game-state-events.md)
- [015 - Migrate Player Actions to Event Bus](tasks/015-migrate-player-actions.md)
- [014 - Migrate Combat Events to Event Bus](tasks/014-migrate-combat-events.md)
- [013 - Event bus & intent system](tasks/013-event-bus-intent-system.md)
- [012 - Player hit animation not playing](tasks/012-player-hit-animation.md)
- [010 - TMX map loading integration](tasks/010-map-loading-integration.md)
- [009 - Combat readability & feedback](tasks/009-combat-readability-feedback.md)
- [008 - Enemy visuals & basic variety](tasks/008-enemy-visuals-variety.md)
- [007 - Player visuals & animation hookup](tasks/007-player-visuals-animation.md)
- [011 - Player facing direction bug (WASD mismatch)](tasks/011-player-facing-direction-bug.md)
- [006 - Enemy smearing investigation & fix](tasks/006-enemy-smearing-investigation.md)
- [005 - ECS enemy waves & combat](tasks/005-ecs-enemy-waves-combat.md)
- [004 - Player ECS migration](tasks/004-player-ecs-migration.md)
- [003 - ECS foundation bootstrap](tasks/003-ecs-foundation-bootstrap.md)
- [002 - Basic enemy wave prototype](tasks/002-basic-enemy-wave-prototype.md)
- [001 - Placeholder assets import](tasks/001-placeholder-assets-import.md)