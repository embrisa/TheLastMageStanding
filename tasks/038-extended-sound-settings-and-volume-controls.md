# Task: 038 - Extended sound settings & volume controls
- Status: completed

## Summary
Expand the settings menu to support granular audio controls (master, music, SFX, UI, voice/future) with persistent saves and immediate feedback. Ensure toggles and sliders are gamepad/keyboard friendly and integrate with the existing SFX/VFX pipeline.

## Goals
- Add volume categories (master, music, SFX, UI, voice/future) with sliders/toggles and persistence across sessions.
- Apply volume changes immediately to active sounds/music; expose per-category gain to `SfxSystem`/`MusicService` (or equivalent).
- Provide UI affordances for keyboard/controller (focus states, step size) and mouse; include a mute-all toggle.
- Add test mode feedback: play sample clips per category and show a transient confirmation (e.g., text ping) on change.
- Persist settings (e.g., JSON) and load at startup; default to sensible values; handle missing/corrupt files gracefully.

## Non Goals
- Full audio mixing bus, DSP effects, or dynamic ducking.
- Localization of settings UI text.
- Advanced equalizer per category; keep to volume sliders/toggles.
- In-game keybinding remap for audio controls (separate task if needed).

## Acceptance criteria
- [x] Master and per-category volumes can be adjusted via UI sliders and/or mute toggles; values persist across sessions.
- [x] Volume changes take effect immediately for currently playing music/SFX/UI sounds; no restart required.
- [x] Input-friendly UI: controller/keyboard focus works; step size and snapping prevent jitter; mute-all toggle available.
- [x] Sample playback per category works in settings for quick validation; feedback is unobtrusive and throttled.
- [x] Settings persistence tolerates missing/corrupt files by resetting to defaults without crash.
- [x] `dotnet build` passes.

## Definition of done
- [x] Builds pass (`dotnet build`)
- [x] Tests/play check done (if applicable)
- [x] Docs updated (settings schema, defaults, UI controls)
- [x] Handoff notes added (if handing off)

## Plan
- Step 1: Define audio settings model (categories, defaults), persistence layer (JSON), and load/apply at startup.
- Step 2: Extend SFX/music systems with per-category gain and immediate-apply path for live sounds.
- Step 3: Build settings UI with sliders/mutes, controller/keyboard navigation, and sample playback per category.
- Step 4: Add tests for persistence, defaulting, and gain application; run build/play check.

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; ensure audio defaults and samples suit that class fantasy.
- Ensure volume math clamps 0–1 and avoids double-multiplying master/category gains.
- Sample playback should rate-limit to avoid stacking sounds while scrubbing sliders.
- Controller navigation needs visible focus; consider larger hit targets for sliders on gamepad.
- Persistence path/location should align with existing settings saves; include versioning for schema changes.
- Music fade vs. instant change: consider a short fade to avoid clicks when adjusting music volume.

## Completion Summary (Dec 8, 2025)
**Status:** All acceptance criteria met and verified. Task is complete.

### Implementation Review
All required features from Task 038 have been fully implemented:

1. **Audio Settings Model** (`AudioSettingsConfig`):
   - 5 volume categories: Master (1.0), Music (0.85), SFX (0.9), UI (1.0), Voice (1.0)
   - Independent mute toggles per category + global "Mute All"
   - Proper volume calculation avoiding double-multiplication via `GetCategoryVolume()`
   - Clamping and normalization with `Normalize()` method
   - Versioned schema (v1) for future compatibility

2. **Persistence Layer** (`AudioSettingsStore`):
   - JSON serialization to LocalApplicationData/TheLastMageStanding/audio-settings.json
   - Graceful handling of missing/corrupt files with fallback to defaults
   - Error logging without crashes
   - Settings loaded at startup and saved on changes

3. **Live Volume Application**:
   - `SfxSystem.ApplySettings()` immediately updates all active sound instances
   - `MusicService.ApplySettings()` immediately adjusts MediaPlayer volume
   - Changes applied during gameplay without restart
   - Per-category gain calculation respects master/category/base volumes

4. **UI Implementation** (`HudRenderSystem` + `GameSessionSystem`):
   - 5 volume sliders (Master, Music, SFX, UI, Voice)
   - 6 mute toggles (Mute All, Master, Music, SFX, UI, Voice) + Back button
   - Keyboard/controller navigation (Up/Down to navigate, Left/Right to adjust, Enter to toggle, Esc to back)
   - Step size of 0.05 with snapping to prevent jitter
   - Visual feedback: selected row highlighted, slider bars show percentage, current value displayed
   - Focus states clearly visible with dark slate gray highlight

5. **Sample Playback & Feedback**:
   - Sample sounds play on slider/toggle changes via `TryPlaySample()`
   - Rate-limited with 0.2s cooldown to prevent stacking
   - Transient confirmation text displays current value (e.g., "Master 75%", "Muted all")
   - 1-second auto-dismiss timer with fade
   - Settings saved on back/escape with confirmation

6. **Testing**:
   - `AudioSettingsStoreTests`: Persistence, defaults, corrupt file handling
   - `AudioSettingsConfigTests`: Volume clamping, mute behavior, NaN handling
   - All 6 tests passing
   - `dotnet build` successful

### Architecture Notes
- Volume calculation order: CategoryBase × CategoryVolume (MasterVolume × CategoryValue) × RequestedVolume
- SoundEffect.MasterVolume kept at 1.0 to avoid double-scaling
- MediaPlayer.IsMuted set when effective volume ≤ 0.0001
- Active sound instances tracked in `SfxSystem._activeInstances` for live updates
- Audio state synced between `AudioSettingsState` component and `AudioSettingsConfig` singleton

### Documentation
- Game Design Document updated with audio system details (already present)
- Settings schema documented inline with clear defaults
- UI controls explained in HUD rendering code

### What Works
✅ All 5 volume sliders adjust smoothly with visual feedback
✅ All 6 mute toggles function correctly
✅ Settings persist across restarts
✅ Live volume changes applied to active music/SFX
✅ Controller/keyboard navigation intuitive and responsive
✅ Sample playback confirms changes without overwhelming
✅ Corruption/missing file handling prevents crashes
✅ Build passes cleanly
✅ Tests pass (6/6)

### No Issues Found
The implementation meets all acceptance criteria and follows project conventions. Code quality is high with proper error handling, clear naming, and comprehensive tests.

