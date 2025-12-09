# Task: 064 - Stats and run history UI
- Status: backlog

## Summary
Implement the stats and run history UI accessible via `npc_archivist` NPC (green square) in the hub. Players can view their meta progression stats, completed stages, best run times, kill counts, deaths, and other achievements. Read-only informational UI.

## Goals
- Create StatsUISystem for displaying player statistics
- Show meta progression: meta level, XP, talent points available
- Show campaign progress: acts/stages completed, locked stages
- Show run history: best times, longest survival, highest wave reached
- Show combat stats: total kills, damage dealt, deaths
- Show equipment stats: items collected, gold earned
- Provide sense of progression and achievement

## Non Goals
- Leaderboards or online comparisons
- Detailed per-run breakdowns (summary only)
- Achievement system (badges, titles)
- Exporting/sharing stats

## Acceptance criteria
- [ ] Pressing E near green NPC (`npc_archivist`) opens stats UI
- [ ] UI shows meta level, XP bar, talent points available
- [ ] Campaign section shows acts/stages completed with icons
- [ ] Run history shows best records (time, wave, survival)
- [ ] Combat stats show kills, deaths, damage totals
- [ ] Equipment stats show items collected, gold earned
- [ ] ESC closes stats UI and returns to hub
- [ ] Data pulls from PlayerProfile persistence
- [ ] `dotnet build` passes; manual playtest confirms correct data

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Create StatsUIState component (IsOpen, SelectedTab, etc.)
- Step 2: Define stats categories: Meta, Campaign, Runs, Combat, Equipment
- Step 3: Create StatsUISystem implementing IUpdateSystem, IUiDrawSystem, ILoadContentSystem
- Step 4: Render stats UI with tabbed/sectioned layout
- Step 5: Wire InteractionInputSystem to toggle StatsUIState on `npc_archivist`
- Step 6: Pull stats data from PlayerProfile and MetaProgressionManager
- Step 7: Add navigation between stat categories (tabs or pages)
- Step 8: Format numbers and percentages for readability
- Step 9: Register system in EcsWorldRunner hub-only systems
- Step 10: Test with various player profiles to verify accuracy

## Notes / Risks / Blockers
- **Dependency**: PlayerProfile tracking (check what stats are currently persisted)
- **Dependency**: MetaProgressionManager (check available data)
- **Risk**: Stats may not be tracked yet (need to add tracking to run completion, combat, etc.)
- **Design**: How granular should run history be? (Last 10 runs? All-time bests only?)
- **UX**: Consider visual charts/graphs for engagement (optional, text is fine for MVP)
