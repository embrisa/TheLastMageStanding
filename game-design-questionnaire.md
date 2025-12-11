Summary of current state vs gaps
Vision: 4-act story ARPG with hub-only build config and in-run level-up choices (docs aligned).
Code: SceneManager/MainMenu→Hub→Stage flow exists; StageRegistry only Act 1 (3 placeholder stages) all mapped to Tiles/Maps/FirstMap; hub map spawns NPCs; stage selection UI present.
In-run choices: Level-up UI shows 3 cards and samples stat boosts + skill modifiers; gameplay pauses until a choice is made.
LevelUpChoiceUISystem.csLines 115-138
var title = "Level Up! Choose one of 3 options";state.Choices = _generator.GenerateChoices(world, state.Player);
Conflicts with design pivot: Perk tree and inventory UIs run in both hub and stage (not gated); level-up offers 3 options (docs say 2); meta cap/unlock tables not enforced; stage maps mostly placeholders; map selection ignores stageId (always Hub/FirstMap).
Decision areas covered
Campaign: acts/stages/bosses/biomes, map reuse, gating.
Progression: meta cap/unlocks, talent respec/points, level-up choice shape, mid-run restrictions.
Combat: skills/dash/aiming, wave pacing, elites/boss telegraphs, status effects.
Hub: NPC interactions, UI access limits, loadouts, shop scope, onboarding.
Systems/Tech: scene transitions, stage map loading, save slots, debug toggles, resolution/content expectations.
Audio/UX: music per scene, settings persistence, level-up feedback, navigation, accessibility.
Comprehensive multiple-choice questionnaire (42)
Campaign
1) Acts and structure?
Answer: 4 acts linear

2) Typical stages per act?
Answer: 8 Stages per act.

3) Stage length/wave budget target?
Answer: 25 waves (1 minute per wave so 25 minutes per stage before stage boss spawns)

4) Boss encounter depth?
Answer: Mini-boss per mid-stage + final boss that has 2 phases with adds/telegraphs

5) Biome uniqueness per act?
Answer: New tilesets/enemies/music each act, some enemies & tiles can be returning

6) Stage unlocking rules?
Answer: Completion-only (no meta gate)

7) Map per stage?
Answer: Unique map per stage

8) Narrative delivery?
Answer: Text popups and Voice Over before/after stages

Progression (meta vs in-run)
9) Meta level cap?
Answer: 60

10) Meta XP sources weighting?
Answer: Stage clear + boss bonus + performance (docs)

11) Talent respec policy?
Answer: Respec in hub with gold cost

12) Talent point source?
Answer: Meta level-ups only

13) Skill unlock cadence?
Answer: Meta-level thresholds per skill

14) Equipment handling mid-run?
Answer:  Hub-only equip; drops auto-collect and Read-only mid-run (view stats only)

15) Level-up choice count?
Answer: 3 options

16) Choice composition per roll?
Answer: Totally random with no restrictions other than the same bonus can't appear more than once per roll.

17) Meta rewards on death vs clear?
Answer: Clear + Performance-scaled.
Future change: Dying should lower the equipments durability by 10% and required a repair (gold cost) to fix when reaching 100% or item become unusable.

18) Dash tuning baseline?
Answer: 0.2s, 150u, 2s CD, 0.15s i-frames
Future change: Dash should be rethemed into a mage skill and work like any other skill and cost mana.

19) Primary Skill hotkeys?
Answer LMB+RMB and Keys 1-6

20) Skill targeting mode?
Answer: Mouse-aimed directional

21) Wave pacing target?
Answer: 1 wave should last 1 minute and each wave should become stronger than before. Cap at 25 waves.

22) Enemy role mix by default?
Answe: Base melee + ranged + chargers + protectors + buffers

23) Elite modifier frequency?
Answer: Elite spawn at; 5, 10, 15, 20. Boss at 25.

24) Boss telegraph & hit-stop intensity?
Answer: colored telegraphs + damage-based hit-stop

25) Status effects scope?
Answer: Fire/Frost/Arcane (burn/shock/freeze/slow/debuffs/stuns/root/buffs)

Hub, NPCs, and UI scope
26) Hub availability?
Answer: Default entry after slot select

27) Stage selection access?
Answer: NPC-driven (arena master) + screen-space UI (current)r

28) Talent/Inventory access during stage?
Answer; View-only allowed, no changes

29) Hub NPC interaction style?
a) Proximity prompt + “E” to open UI (current)
b) Auto-open on proximity
c) Dialogue tree first, then UI
d) Menu-only shortcuts (no NPCs)
e) Other
30) Shop scope?
a) Purchase items only; equip in inventory UI
b) Purchase + auto-equip if slot empty
c) Gacha/roll model
d) Cosmetic-only shop
e) Other
31) Equipment slots?
a) 5 (Weapon, Armor, Amulet, Ring1, Ring2) current doc
b) 4 (merge rings)
c) 6+ (add offhand/charm)
d) Class-dependent slots
e) Other
32) Skill hotbar size?
a) Primary + 4 skills (current)
b) Primary + 3
c) Primary + 5+
d) One-skill-only (auto-attack focus)
e) Other
33) Hub onboarding?
a) Guided first-time tour with prompts (planned)
b) Optional help popups only
c) No tutorial; rely on tooltips
d) Scripted questline through NPCs
e) Other
Systems / Tech
34) Scene transition behavior?
a) Reload map/assets per scene (current)
b) Keep maps resident; soft swap
c) Single-scene streaming
d) Separate processes per scene
e) Other
35) Stage map selection?
a) Per-stage map asset (goal) using stageId
b) Single map per act
c) One global map (env var selects)
d) Procedural map generator
e) Other
36) Save slots?
a) Multi-slot, use most recent as default (current)
b) Single slot only
c) Multi-slot with cloud sync
d) Hardcore slot (deletes on death)
e) Other
37) Debug toggles in shipping builds?
a) Dev-only (strip in release)
b) Hidden cheats allowed in release
c) Always available
d) Build-flag controlled (QA only)
e) Other
38) Virtual resolution/scaling?
a) Keep 960x540 → scale (current)
b) Raise to 1280x720
c) Dynamic resolution scaling
d) Pixel-perfect 1:1 (no scale)
e) Other
Audio / UX
39) Music behavior?
a) Menu music → hub → stage loop (current)
b) Act-specific stage tracks
c) Boss-specific tracks only
d) One global track
e) Other
40) Audio settings persistence?
a) Per-device JSON; apply at boot (current)
b) Per-slot settings
c) Session-only (no save)
d) Cloud/profile sync
e) Other
41) Level-up UX?
a) Pause game, overlay choice UI (current)
b) Slow-mo instead of pause
c) No pause (real-time pick)
d) Queue choices to hub
e) Other
42) UI navigation defaults?
a) Keyboard-first with mouse support (current)
b) Mouse-first (clickable cards/buttons)
c) Controller-first (radial/bumper)
d) Touch-friendly layouts
e) Other
43) Accessibility priorities?
a) Toggles for screen shake/hit-stop, colorblind-friendly telegraphs
b) Minimal options; rely on defaults
c) Extensive suite (remap, text size, narration)
d) Only audio/volume options
e) Other