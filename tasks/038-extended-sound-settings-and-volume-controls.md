# Task: 038 - Extended sound settings & volume controls
- Status: backlog

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
- [ ] Master and per-category volumes can be adjusted via UI sliders and/or mute toggles; values persist across sessions.
- [ ] Volume changes take effect immediately for currently playing music/SFX/UI sounds; no restart required.
- [ ] Input-friendly UI: controller/keyboard focus works; step size and snapping prevent jitter; mute-all toggle available.
- [ ] Sample playback per category works in settings for quick validation; feedback is unobtrusive and throttled.
- [ ] Settings persistence tolerates missing/corrupt files by resetting to defaults without crash.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (settings schema, defaults, UI controls)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define audio settings model (categories, defaults), persistence layer (JSON), and load/apply at startup.
- Step 2: Extend SFX/music systems with per-category gain and immediate-apply path for live sounds.
- Step 3: Build settings UI with sliders/mutes, controller/keyboard navigation, and sample playback per category.
- Step 4: Add tests for persistence, defaulting, and gain application; run build/play check.

## Notes / Risks / Blockers
- Ensure volume math clamps 0–1 and avoids double-multiplying master/category gains.
- Sample playback should rate-limit to avoid stacking sounds while scrubbing sliders.
- Controller navigation needs visible focus; consider larger hit targets for sliders on gamepad.
- Persistence path/location should align with existing settings saves; include versioning for schema changes.
- Music fade vs. instant change: consider a short fade to avoid clicks when adjusting music volume.***

