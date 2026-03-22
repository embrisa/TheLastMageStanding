# Task: 081 - Hitbox debug visualization
- Status: done

## Summary
Make development-time combat spaces visible during stage gameplay so attack ranges and damageable bodies can be inspected directly while playing.

## Goals
- Extend the existing F3 debug overlay to clearly distinguish world/body collision from combat hitboxes.
- Show damageable hurtboxes and active attack/projectile hitboxes in world space without changing gameplay behavior.
- Keep the implementation inside the existing ECS debug rendering path.

## Non Goals
- Adding a separate debug menu or new runtime toggle beyond the existing F3 flow.
- Changing combat logic, hit detection rules, or collider sizing.
- Adding production-facing UI.

## Acceptance criteria
- [x] Pressing F3 during a stage shows active hurtboxes for player/enemies in addition to collision geometry.
- [x] Active melee hitboxes and projectile hitboxes are visually distinct from body colliders.
- [x] Ownership lines and existing movement/separation debug vectors still render.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Extend `CollisionDebugRenderSystem` to render hurtboxes and combat hitboxes with clear color coding.
- Step 2: Update debug logging/docs to reflect the richer F3 overlay and verify with `dotnet build`.

## Notes / Risks / Blockers
- Hurtboxes currently reuse each entity's collider footprint, so the overlay visualizes the effective damageable area as defined by the collider component.

## Handoff
- Current status: complete.
- Files changed: `src/Game/Core/Ecs/Systems/Collision/CollisionDebugRenderSystem.cs`, `src/Game/Core/Ecs/Systems/DebugInputSystem.cs`, `docs/game-design-document.md`, `TASKS.md`, `tasks/081-hitbox-debug-visualization.md`.
- Tests/build/manual checks run: `dotnet build`.
- Next concrete step: run `dotnet run --project src/Game` and verify F3 readability in a live combat scene if a manual polish pass is needed.
- Decisions made: reused the existing F3 renderer instead of creating a new debug system; hurtboxes render from existing collider geometry to match the actual damageable footprint; the overlay now starts enabled by default and can still be toggled with `F3`.
- Risks/blockers: the overlay is intentionally dense during busy fights because it favors correctness over presentation for development use.
