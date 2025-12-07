# Task: 001 - Placeholder assets import
- Status: done

## Summary
Copy and rename selected assets from the old project (`/Users/philippetillheden/untitledGame`) into the new MonoGame project as initial placeholders for visuals/audio.

## Goals
- Identify a minimal placeholder set (sprites, tiles, fonts, audio) suitable for early prototypes.
- Copy assets into `src/Game/Content` with clear naming and folder structure.
- Update content pipeline references so assets build with the game.

## Non Goals
- Final art, animation polish, or audio mixing.
- Gameplay feature work or rendering integration beyond verifying asset load/build.

## Acceptance criteria
- [x] Placeholder assets copied into appropriate subfolders under `src/Game/Content`.
- [x] Names follow the new project’s conventions and avoid collisions.
- [x] `Content.mgcb` updated to include the new assets and builds successfully.
- [x] README/docs note the placeholder asset import source and purpose.

## Definition of done
- Builds pass (`dotnet build`)
- Content builds (`dotnet mgcb /@Content.mgcb ...`)
- Docs updated where relevant
- Handoff notes added (if handing off)

## Plan
- Copy the exact files listed below from `/Users/philippetillheden/untitledGame/assets` into matching `src/Game/Content` subfolders.
- Keep names as listed; only adjust casing/paths to fit the destination layout noted below.
- Register every copied file in `Content.mgcb` with appropriate processor defaults.
- Add a short doc note in `README.md` about placeholder source and scope; verify content + solution build.

## Files to import (source → destination)
- Cursors → `src/Game/Content/Sprites/ui/`
  - `assets/cursor/cursor_click.png`
  - `assets/cursor/cursor_default.png`
  - `assets/cursor/cursor_hover.png`
- Fonts → `src/Game/Content/Fonts/`
  - `assets/fonts/font_regular_text.otf`
  - `assets/fonts/font_regular_title.otf`
  - `assets/fonts/font_stylized_title.otf`
- UI → `src/Game/Content/Sprites/ui/`
  - `assets/ui/button_brown.png`
  - `assets/ui/panel_brown.png`
  - `assets/ui/panel_brown_damaged_dark.png`
  - `assets/ui/panel_brown_dark_corners_b.png`
- Icons → `src/Game/Content/Sprites/icons/`
  - `assets/icons/icon_dash_arcane.png`
  - `assets/icons/icon_dash_fire.png`
  - `assets/icons/icon_dash_frost.png`
  - `assets/icons/icon_heal_arcane.png`
  - `assets/icons/icon_heal_fire.png`
  - `assets/icons/icon_heal_frost.png`
  - `assets/icons/icon_play.png`
  - `assets/icons/icon_player_dash.png`
  - `assets/icons/icon_player_fireball.png`
  - `assets/icons/icon_quit.png`
  - `assets/icons/icon_settings.png`
  - `assets/icons/icon_shield_generic.png`
- Icons — abilities → `src/Game/Content/Sprites/icons/abilities/`
  - `assets/icons/abilities/arcane/arcane_pulse.png`
  - `assets/icons/abilities/arcane/star_cascade.png`
  - `assets/icons/abilities/fire/cinder_burst.png`
  - `assets/icons/abilities/fire/flame_wave.png`
  - `assets/icons/abilities/frost/glacial_spike.png`
  - `assets/icons/abilities/frost/ice_shards.png`
- Images → `src/Game/Content/Sprites/ui/`
  - `assets/images/menu_background.png`
- Objects → `src/Game/Content/Sprites/objects/`
  - `assets/objects/experience_shard.png`
- Sounds → `src/Game/Content/Audio/`
  - `assets/sounds/gameplay_on_player_death.wav`
  - `assets/sounds/stage_1_music.mp3`
  - `assets/sounds/start_screen_music.mp3`
  - `assets/sounds/user_interface_on_click.wav`
  - `assets/sounds/user_interface_on_hover.wav`
- Sprites — abilities → `src/Game/Content/Sprites/abilities/`
  - `assets/sprites/abilities/arcane/arcane_pulse/animation.png`
  - `assets/sprites/abilities/arcane/star_cascade/animation.png`
  - `assets/sprites/abilities/fire/cinder_burst/animation.png`
  - `assets/sprites/abilities/fire/flame_wave/animation.png`
  - `assets/sprites/abilities/frost/frost_focus/Explode.png`
  - `assets/sprites/abilities/frost/frost_focus/Forming.png`
  - `assets/sprites/abilities/frost/frost_focus/Moving.png`
  - `assets/sprites/abilities/frost/glacial_spike/animation.png`
  - `assets/sprites/abilities/frost/ice_shards/animation.png`
- Sprites — player → `src/Game/Content/Sprites/player/`
  - `assets/sprites/player_character/Attack1.png`
  - `assets/sprites/player_character/Attack2.png`
  - `assets/sprites/player_character/Attack3.png`
  - `assets/sprites/player_character/Attack4.png`
  - `assets/sprites/player_character/Attack5.png`
  - `assets/sprites/player_character/AttackRun.png`
  - `assets/sprites/player_character/AttackRun2.png`
  - `assets/sprites/player_character/CrouchIdle.png`
  - `assets/sprites/player_character/CrouchRun.png`
  - `assets/sprites/player_character/Die.png`
  - `assets/sprites/player_character/Idle.png`
  - `assets/sprites/player_character/Idle2.png`
  - `assets/sprites/player_character/Idle3.png`
  - `assets/sprites/player_character/Idle4.png`
  - `assets/sprites/player_character/Run.png`
  - `assets/sprites/player_character/RunBackwards.png`
  - `assets/sprites/player_character/Special1.png`
  - `assets/sprites/player_character/StrafeLeft.png`
  - `assets/sprites/player_character/StrafeRight.png`
  - `assets/sprites/player_character/TakeDamage.png`
  - `assets/sprites/player_character/Taunt.png`
  - `assets/sprites/player_character/Walk.png`
  - `assets/sprites/player_character/Projectile/Explode.png`
  - `assets/sprites/player_character/Projectile/Moving.png`
- Sprites — enemy (bone hexer) → `src/Game/Content/Sprites/enemies/bone_hexer/`
  - `assets/sprites/enemy_bone_hexer/Attack1.png`
  - `assets/sprites/enemy_bone_hexer/Attack2.png`
  - `assets/sprites/enemy_bone_hexer/Attack3.png`
  - `assets/sprites/enemy_bone_hexer/Attack4.png`
  - `assets/sprites/enemy_bone_hexer/Attack5.png`
  - `assets/sprites/enemy_bone_hexer/AttackRun.png`
  - `assets/sprites/enemy_bone_hexer/AttackRun2.png`
  - `assets/sprites/enemy_bone_hexer/CrouchIdle.png`
  - `assets/sprites/enemy_bone_hexer/CrouchRun.png`
  - `assets/sprites/enemy_bone_hexer/Die.png`
  - `assets/sprites/enemy_bone_hexer/Idle.png`
  - `assets/sprites/enemy_bone_hexer/Idle2.png`
  - `assets/sprites/enemy_bone_hexer/Idle3.png`
  - `assets/sprites/enemy_bone_hexer/Idle4.png`
  - `assets/sprites/enemy_bone_hexer/Run.png`
  - `assets/sprites/enemy_bone_hexer/RunBackwards.png`
  - `assets/sprites/enemy_bone_hexer/Special1.png`
  - `assets/sprites/enemy_bone_hexer/StrafeLeft.png`
  - `assets/sprites/enemy_bone_hexer/StrafeRight.png`
  - `assets/sprites/enemy_bone_hexer/TakeDamage.png`
  - `assets/sprites/enemy_bone_hexer/Taunt.png`
  - `assets/sprites/enemy_bone_hexer/Walk.png`
- Tilesets → `src/Game/Content/Tiles/`
  - `assets/tilesets/hub_atlas.png`

## Notes / Risks / Blockers
- Watch for pipeline processing settings differences (e.g., sprite fonts, audio formats).
- Placeholder assets are sourced from `/Users/philippetillheden/untitledGame/assets` for early prototypes and will need replacement with final art/audio later.
- `gameplay_on_player_death.wav` was re-encoded to standard PCM 16-bit so the pipeline can consume it.
- Additional placeholder sprite sets pulled in after initial import: enemies (`BoneLich`, `CryptScuttler`, `GraveMage`, `PlagueWarg`) and NPCs (`AbilityLoadoutNpc`, `ArchivistNpc`, `ArenaMasterNpc`, `TomeScribeNpc`, `VendorNpc`).

