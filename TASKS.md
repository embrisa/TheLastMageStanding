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

## Active tasks
- [029 - Unified stat and damage model](tasks/029-unified-stat-and-damage-model.md)
- [030 - Loot and equipment foundations](tasks/030-loot-and-equipment-foundations.md)
- [031 - Talent/perk tree](tasks/031-talent-perk-tree.md)
- [032 - Elites/boss waves & rewards](tasks/032-elites-boss-waves-and-rewards.md)
- [033 - Dash/defense moves & i-frames](tasks/033-dash-defense-moves-and-i-frames.md)
- [034 - Status effects & elemental interactions](tasks/034-status-effects-and-elemental-interactions.md)
- [035 - Enemy AI variants & squad behaviors](tasks/035-enemy-ai-variants-and-squad-behaviors.md)
- [036 - Elite modifiers & mutators](tasks/036-elite-modifiers-and-mutators.md)
- [037 - Meta progression & run tracking (MVP)](tasks/037-meta-progression-and-run-tracking.md)
- [038 - Extended sound settings & volume controls](tasks/038-extended-sound-settings-and-volume-controls.md)

## Done
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
- [001 - Placeholder assets import](tasks/001-placeholder-assets-import.md)
- [002 - Basic enemy wave prototype](tasks/002-basic-enemy-wave-prototype.md)

