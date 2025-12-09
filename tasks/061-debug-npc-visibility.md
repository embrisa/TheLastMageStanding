# Task: 061 - Debug NPC visibility and spawning
- Status: in_progress

## Summary
NPCs are not visible in the hub after Task 051 implementation. Need to diagnose why NpcSpawnSystem isn't being called or why NPCs aren't rendering, and fix the issue so players can see and interact with hub NPCs.

## Goals
- Determine why NPCs don't appear in hub
- Fix NPC spawning if broken
- Ensure NpcRenderSystem draws NPCs correctly
- Verify player can see proximity prompts when near NPCs

## Non Goals
- Final NPC sprite art (colored squares are acceptable)
- NPC animations
- Multiple NPC types or states

## Acceptance criteria
- [ ] Console logs show NPC spawning when hub loads
- [ ] 5 colored square NPCs visible in hub at correct positions from HubMap.tmx
- [ ] Walking near NPCs shows "E - [Action]" proximity prompts
- [ ] Pressing E near NPCs triggers appropriate UI (talent tree, stage selection, etc.)
- [ ] `dotnet build` passes
- [ ] Manual playtest confirms NPCs visible and interactable

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Add debug logging to verify SpawnHubNpcs is called in Game1.cs
- Step 2: Add debug logging to NpcSpawnSystem.Update to see if NPCs are being created
- Step 3: Check if HubMap.tmx object layer exists and has correct NPC markers
- Step 4: Verify NpcRenderSystem is registered and runs in hub-only draw systems
- Step 5: Check if NpcRenderSystem.LoadContent is being called and _whitePixel is created
- Step 6: Add debug rectangle to verify camera viewport and NPC world positions
- Step 7: Verify hub scene is actually loading (not defaulting to stage scene)
- Step 8: Fix identified issue(s)
- Step 9: Remove debug logging after verification

## Notes / Risks / Blockers
- **Possible causes**:
  - SpawnHubNpcs not being called when hub scene loads
  - NpcSpawnSystem running before map is loaded
  - NPC entities created but render system not drawing them
  - NPCs spawning outside camera viewport
  - Scene starting in Stage instead of Hub
  - LoadContent not called on NpcRenderSystem
- **Debug approach**: Add console logs at each stage of NPC lifecycle (spawn, update, draw)
