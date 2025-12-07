# Task: 011 - Player facing direction bug (WASD mismatch)
- Status: done

## Summary
WASD inputs map to incorrect facing rows in the player animation. After recent sprite hookups, pressing A/D produces SouthWest/South facings instead of West/East. Needs investigation and fix so facing rows align with input.

## Goals
- Reproduce the mismatch quickly (A → West row, D → East row should be correct).
- Identify and fix the facing calculation and/or row mapping so WASD + diagonals match expected directions.
- Verify idle/run animations pick the correct row and frames without regressions to other facings.

## Non Goals
- New animations or content changes.
- Broader movement/combat logic changes beyond facing selection.

## Acceptance criteria
- Pressing W/S uses North/South rows; pressing A/D uses West/East rows; diagonals use their correct rows.
- No duplicated/offset sprites; single correct frame per facing.
- `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`).
- Manual playcheck confirms WASD/diagonals map to correct facing rows.
- Task notes updated; handoff info provided if not merged.

## Plan
- Reproduce: run game, verify current incorrect mappings for A/D.
- Inspect facing logic (`PlayerRenderSystem.ToFacing`, row mapping) and adjust to match row order (Row1=East, Row2=SE, Row3=South, Row4=SW, Row5=West, Row6=NW, Row7=North, Row8=NE).
- Validate diagonals and idle/run clip selection; adjust tests or logging if needed.
- Playcheck for all directions; build.

## Notes / Risks / Blockers
- Fix: normalized movement now snaps to octants; West/East use the run clip (not strafe) so A/D map to Row5/Row1 correctly.
- Tests: `dotnet build`; manual playcheck confirmed by user that A/D now face West/East as expected.
- Input system normalizes WASD; Y is negative for up.


