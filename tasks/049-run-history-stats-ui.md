# Task: 049 - Run History & Stats Display UI
- Status: backlog

## Summary
Create UI for viewing run history, aggregate stats, and personal records. Display recent runs with detailed stats, highlight personal bests, and show interesting insights about player performance over time.

## Goals
- Create run history UI showing recent runs (date, wave, duration, outcome).
- Display detailed stats for selected run (kills, damage, gold, equipment found, etc.).
- Show personal records (best wave, fastest run, most kills, etc.).
- Display aggregate stats (total runs, average wave, total kills, time played).
- Highlight trends and insights (e.g., "Most dangerous enemy: Ranged Imp").
- Support pagination or scrolling for long history.

## Non Goals
- Leaderboards or social features (no online comparison).
- Advanced analytics or graphs (keep simple for MVP).
- Filtering/sorting by multiple criteria (just chronological for MVP).
- Exporting run data (defer to future).

## Acceptance criteria
- [ ] Run history UI displays last 20-50 runs in chronological order.
- [ ] Each run entry shows: date, wave reached, duration, kills, gold, outcome.
- [ ] Clicking a run shows detailed stats panel.
- [ ] Personal records section shows: best wave, longest run, most kills, most gold.
- [ ] Aggregate stats section shows: total runs, total kills, total time, average wave.
- [ ] UI highlights new personal records (e.g., "New best wave!").
- [ ] Navigation from meta hub "Run History" button.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Play check done (can view history, see stats, identify records)
- Docs updated (run history UI, stats display)
- Handoff notes added (if handing off)

## Plan

### Step 1: Run History Service Integration (0.5 day)
- Use `RunHistoryService` from Task 037:
  - `GetRecentRuns(int count)` → load last N runs
  - `GetPersonalRecords(PlayerProfile profile)` → compute best wave, longest run, etc.
  - `GetAggregateStats(PlayerProfile profile)` → compute totals and averages
- Add methods if needed:
  - `GetRunById(string id)` → load specific run for detailed view
  - `GetHighlights(PlayerProfile profile)` → interesting insights (most used skill, deadliest enemy)
- Ensure run data includes all relevant fields (from Task 037 `RunSession` model)

### Step 2: Run History List UI (1 day)
- Create `UI/MetaHub/RunHistoryUI.cs`:
  - Main panel: List of recent runs
    - Each entry shows:
      - Date/time (e.g., "Dec 8, 2025 - 10:30 AM")
      - Wave reached (e.g., "Wave 15")
      - Duration (e.g., "23m 45s")
      - Kills, gold collected
      - Outcome icon (victory/death)
    - Highlight personal records with badge (e.g., "Best Wave!")
  - Scrollable list or pagination (show 20 runs per page)
  - Click run entry → show detailed stats panel (Step 3)
- Style: consistent with other meta hub UI
- Add "Back to Hub" button

### Step 3: Detailed Run Stats Panel (1 day)
- Create detailed view when run is selected:
  - Summary:
    - Wave reached, duration, outcome
    - Total kills, damage dealt, damage taken, gold collected
  - Breakdown:
    - Kills by enemy type (if tracked)
    - Equipment found (list of items)
    - Skills used (if tracked)
    - Cause of death (enemy type, damage type)
  - Meta rewards:
    - Meta XP earned
    - Gold added to profile
  - Visual: panel overlay or side panel
- Add "Close" button to return to history list

### Step 4: Personal Records Section (0.5 day)
- Add personal records section to run history UI:
  - Display:
    - Best wave reached (and date achieved)
    - Longest run duration (and date)
    - Most kills in single run (and date)
    - Most gold in single run (and date)
    - Fastest run to wave X (e.g., wave 10 in 10 minutes)
  - Click record → jump to that run in history (if available)
  - Highlight new records after recent run (e.g., "New best wave!")
- Style: prominent section at top or side of UI

### Step 5: Aggregate Stats Section (0.5 day)
- Add aggregate stats section:
  - Display:
    - Total runs completed
    - Total deaths
    - Total time played (hours/minutes)
    - Total kills
    - Total gold earned
    - Average wave reached
    - Average run duration
    - Favorite skills (most used)
    - Deadliest enemy (most deaths from)
  - Style: summary panel or dashboard view
- Add interesting insights (optional):
  - "You've survived longest on Sundays" (if date/time tracked)
  - "Your average wave is improving!" (if trend detected)

### Step 6: Visuals & Polish (0.5 day)
- Add UI polish:
  - Hover effects on run entries
  - Highlight selected run
  - Badge icons for personal records (trophy, star)
  - Color-code outcomes (green for victory, red for death)
  - Animate new record notifications (slide-in, glow)
- Add graphs/charts (optional for MVP):
  - Simple line graph: wave reached over time
  - Bar chart: kills per run
  - Defer if too complex; keep text-based for MVP

### Step 7: Testing & Documentation (0.5 day)
- Test run history:
  - View history with multiple runs
  - Click run, view detailed stats
  - Verify pagination/scrolling works
- Test personal records:
  - Complete run that sets new record
  - Verify record updates correctly
  - Verify date tracking
- Test aggregate stats:
  - Verify totals and averages are accurate
  - Check with edge cases (0 runs, 1 run, many runs)
- Document:
  - Run history UI flow
  - Stats calculation logic
  - Personal records tracking
- Update `game-design-document.md` with run history section
- Run `dotnet build` and fix errors

## Estimated Timeline
- **Total: 2-3 days**

## Dependencies
- Task 037: Meta progression foundations (run history service, run session data)
- Task 045: Meta hub UI (navigation integration)

## Notes / Risks / Blockers
- Run history storage: Ensure `RunHistoryService` stores enough data for detailed stats. May need to extend `RunSession` model.
- Personal records calculation: Compute on-demand vs. cache? Compute on-demand is simpler for MVP; optimize later if needed.
- Aggregate stats: Some stats (like "deadliest enemy") require detailed tracking. Start with basic stats, expand later.
- UI layout: Balance between showing lots of info and keeping it readable. Prioritize key stats, hide details in expandable sections.
- Graph/chart library: If graphs are desired, consider using a chart library (e.g., LiveCharts for MonoGame). Defer to polish phase if time constrained.
