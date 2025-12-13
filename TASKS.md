# Task Documentation & Workflow

Use this file to track active work items. Each task should be created by copying `TASK_TEMPLATE.md` and filling in the fields. Keep entries concise and status-driven so agents can pick up quickly.

## Status labels
- `backlog` — not started, ready to be picked.
- `in_progress` — actively being worked on.
- `blocked` — needs input/unblocks.
- `in_review` — PR opened; awaiting review/testing.
- `done` — merged/completed; verify acceptance criteria.

## Required fields per task
- Title/ID
- Summary and rationale
- Acceptance criteria (observable behaviors/outputs)
- Definition of done (tests/builds, docs, handoff)
- Owner + branch/PR links when available
- Notes/risks/blockers

## Workflow
1. Copy `TASK_TEMPLATE.md` into a new section below and fill it out.
2. Update status + notes daily; include latest commit/branch if WIP.
3. When opening a PR, link it and restate acceptance criteria in the PR body.
4. On completion, verify acceptance criteria and testing notes, then mark `done`.
5. If handing off, leave next steps, open questions, and any gotchas.

## Active tasks (In Progress)


## Backlog
- [ ] [Task: 049 - Run History & Stats Display UI](tasks/049-run-history-stats-ui.md)
- [ ] [Task: 052 - Stage/act campaign system](tasks/052-stage-act-campaign-system.md)
- [ ] [Task: 054 - Meta progression system updates](tasks/054-meta-progression-updates.md)
- [ ] [Task: 055 - Skill selection hub UI](tasks/055-skill-selection-hub-ui.md)
- [ ] [Task: 056 - Equipment management hub UI](tasks/056-equipment-management-hub-ui.md)
- [ ] [Task: 062 - Skill selection UI in hub](tasks/062-skill-selection-ui.md)
- [ ] [Task: 063 - Shop UI and equipment purchasing](tasks/063-shop-ui-equipment-purchasing.md)
- [ ] [Task: 064 - Stats and run history UI](tasks/064-stats-run-history-ui.md)
- [ ] [Task: 065 - Hub menu actions (settings and quit)](tasks/065-hub-menu-actions.md)
- [ ] [Task: 066 - NPC visual improvements](tasks/066-npc-visual-improvements.md)
- [ ] [Task: 067 - Hub map environmental polish](tasks/067-hub-map-environmental-polish.md)
- [ ] [Task: 068 - Hub tutorial and first-time UX](tasks/068-hub-tutorial-first-time-ux.md)

## Blocked / Superseded / Other
- [ ] [Task: 030: Loot and Equipment Foundations - Summary](tasks/030-SUMMARY.md) - unknown
- [ ] [Task: 031 - Talent/Perk Tree Implementation Summary](tasks/031-SUMMARY.md) - unknown
- [ ] [Task: 042 — Skill Selection & Equipping UI](tasks/042-skill-selection-ui.md) - ** SUPERSEDED by Task 055
- [ ] [Task: 043 — Skill Unlock Progression](tasks/043-skill-unlock-progression.md) - ** SUPERSEDED by Task 054
- [ ] [Task: 045 - Meta Hub UI & Scene](tasks/045-meta-hub-ui-and-scene.md) - BLOCKED (overlaps with Task 051)
- [ ] [Task: 046 - Shop & Equipment Purchase UI](tasks/046-shop-and-equipment-ui.md) - NEEDS UPDATE (Task 056 covers equipment UI)
- [ ] [Task: 047 - Talent Tree Integration & Application](tasks/047-talent-tree-integration.md) - NEEDS UPDATE + BLOCKED by Task 051
- [ ] [Task: 048 - In-Run Inventory & Equipment Swapping](tasks/048-in-run-inventory-ui.md) - CONFLICTS WITH NEW DESIGN - RECONSIDER
- [ ] [Hub Completion Arc — Task Summary](tasks/HUB_COMPLETION_ARC.md) - : Blocking all other hub tasks

## Completed
- [x] [Task 044 — Skill Balance & Feel Pass](tasks/044-skill-balance-and-feel-pass.md)
- [x] [Task: 001 - Placeholder assets import](tasks/001-placeholder-assets-import.md)
- [x] [Task: 002 - Basic enemy wave prototype](tasks/002-basic-enemy-wave-prototype.md)
- [x] [Task: 003 - ECS foundation bootstrap](tasks/003-ecs-foundation-bootstrap.md)
- [x] [Task: 004 - Player ECS migration](tasks/004-player-ecs-migration.md)
- [x] [Task: 005 - ECS enemy waves & combat](tasks/005-ecs-enemy-waves-combat.md)
- [x] [Task: 006 - Enemy smearing investigation & fix](tasks/006-enemy-smearing-investigation.md)
- [x] [Task: 007 - Player visuals & animation hookup](tasks/007-player-visuals-animation.md)
- [x] [Task: 008 - Enemy visuals & basic variety](tasks/008-enemy-visuals-variety.md)
- [x] [Task: 009 - Combat readability & feedback](tasks/009-combat-readability-feedback.md)
- [x] [Task: 010 - TMX map loading integration](tasks/010-map-loading-integration.md)
- [x] [Task: 011 - Player facing direction bug (WASD mismatch)](tasks/011-player-facing-direction-bug.md)
- [x] [Task: 012 - Player hit animation not playing](tasks/012-player-hit-animation.md)
- [x] [Task: 013 - Event bus & intent system](tasks/013-event-bus-intent-system.md)
- [x] [Task: 014 - Migrate Combat Events to Event Bus](tasks/014-migrate-combat-events.md)
- [x] [Task: 015 - Migrate Player Actions to Event Bus](tasks/015-migrate-player-actions.md)
- [x] [Task: 016 - Game State Events](tasks/016-game-state-events.md)
- [x] [Task: 017 - Directional TakeDamage animation](tasks/017-directional-takedamage-animation.md)
- [x] [Task: 018 - Game over & wave HUD](tasks/018-game-over-and-wave-hud.md)
- [x] [Task: 019 - Session stats and run summary](tasks/019-session-stats-and-run-summary.md)
- [x] [Task: 020 - Pause and settings overlay](tasks/020-pause-and-settings-overlay.md)
- [x] [Task: 021 - Ranged enemy and projectiles](tasks/021-ranged-enemy-and-projectiles.md)
- [x] [Task: 022 - XP orbs and level-ups](tasks/022-xp-orbs-and-level-ups.md)
- [x] [Task: 023 - Collision system foundation](tasks/023-collision-system-foundation.md)
- [x] [Task: 024 - Static world collision](tasks/024-static-world-collision.md)
- [x] [Task: 025 - Dynamic actor separation & knockback](tasks/025-dynamic-actor-separation-and-knockback.md)
- [x] [Task: 026 - Collider-driven combat hits](tasks/026-collider-driven-combat-hits.md)
- [x] [Task: 027 - Animation events & directional hitboxes](tasks/027-animation-events-and-directional-hitboxes.md)
- [x] [Task: 028 - Telegraphs, VFX/SFX, and hit-stop](tasks/028-telegraphs-vfx-sfx-hit-stop.md)
- [x] [Task: 029 - Unified stat and damage model](tasks/029-unified-stat-and-damage-model.md)
- [x] [Task: 030 - Loot and equipment foundations](tasks/030-loot-and-equipment-foundations.md)
- [x] [Task: 031 - Talent/perk tree](tasks/031-talent-perk-tree.md)
- [x] [Task: 032 - Elites/boss waves & rewards](tasks/032-elites-boss-waves-and-rewards.md)
- [x] [Task: 033 - Dash/defense moves & i-frames](tasks/033-dash-defense-moves-and-i-frames.md)
- [x] [Task: 034 - Status effects & elemental interactions](tasks/034-status-effects-and-elemental-interactions.md)
- [x] [Task: 035 - Enemy AI variants & squad behaviors](tasks/035-enemy-ai-variants-and-squad-behaviors.md)
- [x] [Task: 036 - Elite modifiers & mutators](tasks/036-elite-modifiers-and-mutators.md)
- [x] [Task: 037 - Meta progression foundations](tasks/037-meta-progression-and-run-tracking.md)
- [x] [Task: 038 - Extended sound settings & volume controls](tasks/038-extended-sound-settings-and-volume-controls.md)
- [x] [Task: 039 - Mage Skill System Implementation Summary](tasks/039-SUMMARY.md)
- [x] [Task: 039 - Mage skill system](tasks/039-skill-system.md)
- [x] [Task: 040 — Skill Hotbar UI](tasks/040-skill-hotbar-ui.md)
- [x] [Task: 041 — Skill Hotkey Input (Keys 1-4)](tasks/041-skill-hotkey-input.md)
- [x] [Task: 050 - Level-up choice system](tasks/050-level-up-choice-system.md)
- [x] [Task: 051 - Hub scene and scene management](tasks/051-hub-scene-and-scene-management.md)
- [x] [Task: 053 - Remove mid-run configuration access](tasks/053-remove-mid-run-configuration.md)
- [x] [Task: 057 — Mouse-Aimed Skill Targeting](tasks/057-mouse-aimed-skill-targeting.md)
- [x] [Task: Port main menu to Myra UI](tasks/070-main-menu-myra-port.md)
- [x] [Task: Mouse pointer offset on 4K macOS windowed mode](tasks/074-mouse-pointer-offset.md)
- [x] [Task: 061 - Debug NPC visibility and spawning](tasks/061-debug-npc-visibility.md) - in_progress
