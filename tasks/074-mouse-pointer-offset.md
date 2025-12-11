# Task: Mouse pointer offset on 4K macOS windowed mode
- Status: completed

## Summary
Mouse cursor alignment is incorrect in-game on macOS (4K display, windowed mode). OS cursor and in-game targeting/highlights do not match; main menu hover/clicks and in-run aiming are offset, so projectiles and UI selections register away from the visible cursor.

## Goals
Clarify and resolve mouse coordinate scaling so UI hit tests and gameplay aiming match the OS cursor across resolutions/HiDPI (notably macOS Retina 4K, windowed mode).

## Non Goals
- Broader input refactors or controller support.
- UI redesign.
- Changing render scale/virtual resolution beyond what is needed for correct mouse alignment.

## Acceptance criteria
- [x] On macOS 4K windowed mode, OS cursor position matches menu hover/selection and clicks.
- [x] In gameplay, aiming and projectiles/skills land at the visible cursor position in all directions.
- [x] Behavior remains correct on non-HiDPI displays and standard 1080p tests.
- [x] Manual check confirms no regression for existing input flows (movement, pause, etc.).

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Reproduce on macOS 4K windowed mode; record exact offsets vs. cursor.
- Step 2: Inspect MonoGame mouse coordinate sources (Mouse.GetState, Viewport, backbuffer sizes) and how they map to virtual resolution rendering pipeline.
- Step 3: Adjust scaling or transform (e.g., account for Retina scaling or backbuffer vs. window size) and validate across resolutions.
- Step 4: Regression test menus and in-run aiming on multiple resolutions/DPIs.

## Notes / Risks / Blockers
- Environment: macOS, 4K display, windowed mode; likely Retina backbuffer scaling mismatch.
- Attempts so far: scaled mouse coords from viewport to virtual resolution; tried window-size guard for HiDPI; reverted to viewport-only scaling. Both still show offsets (menu hover/clicks and aiming).
- Rendering pipeline: virtual resolution 960x540 upscaled via render target; camera/world use ScreenToWorld; UI hit tests rely on `InputState.MouseScreenPosition`.
- Need to confirm whether MonoGame on macOS returns raw window points vs. backbuffer pixels on HiDPI and whether `GraphicsDevice.Viewport` matches `Mouse.GetState()` coordinates.
- **Resolution**: Switched mouse coordinate scaling to use `Window.ClientBounds` (logical points) instead of `GraphicsDevice.Viewport` (physical pixels). This aligns with `Mouse.GetState()` returning logical points on macOS. Also updated `Game1.Draw` to use `GraphicsDevice.Viewport` for the destination rectangle to ensure correct rendering on HiDPI backbuffers.
- **UI Fixes**: Updated `MainMenuScreen` to use virtual resolution (960x540) for hit testing and layout instead of backbuffer viewport, ensuring mouse clicks register correctly. Corrected `HubMenuSystem` to use 960x540 virtual resolution instead of 1280x720.
- **Myra Fix**: Updated `MyraMainMenuScreen` to scale the `Desktop` based on `Window.ClientBounds` vs Virtual Resolution, and moved Myra rendering to the backbuffer in `Game1.Draw`. This ensures Myra's input handling (Window Space) matches its rendering (Scaled Window Space).



