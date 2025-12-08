# Task: 033 - Dash/defense moves & i-frames
- Status: done

## Summary
Add a responsive defensive move (dash/evade/block) with brief invulnerability frames, stamina/cooldown gating, and telegraphed timing. Integrate with animation events, camera nudge, and the collision/hit-stop stack so players can avoid burst pressure from elites and projectiles.

## Goals
- Implement a dash/evade action with configurable distance, speed curve, and i-frame window, driven by animation events.
- Add gating via stamina bar or cooldown (configurable), with UI feedback and input buffering.
- Integrate with collision: allow passing through enemies during i-frames, but respect world-static colliders; clamp end position.
- Hook in readability: telegraph cue on start/end, camera nudge, optional afterimage VFX, and SFX.
- Cover tests for cooldown/stamina, i-frame correctness, collision immunity, and end-position clamping.

## Non Goals
- Complex parry/counter mechanics or shield directions.
- Perfect block timing bonuses beyond basic i-frame avoidance.
- Netcode/rollback considerations.
- New animation authoring tools (reuse current animation event system).

## Acceptance criteria
- [x] Player can dash/evade with a defined i-frame window; collisions with enemies/projectiles do not deal damage during i-frames.
- [x] Dash respects world geometry: no tunneling through static colliders; end position is clamped or slides along walls.
- [x] Gating works: dash is limited by cooldown or stamina; UI shows availability; input buffering works within a short window.
- [x] Telegraph/FX: visible start/end cue; optional camera nudge; SFX plays; debug overlay can show i-frame window when enabled.
- [x] Tests cover cooldown/stamina gating, i-frame immunity, and collision clamping; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (dash config, i-frame rules, UI controls)
- Handoff notes added (if handing off)

## Completion notes
- Implemented dash components (config/state/cooldown/buffer, `Invulnerable` tag), input buffering on Shift/Space, execution/movement systems, HUD cooldown indicator, debug overlay toggle (F9), VFX/SFX hooks, and camera nudge reuse.
- Damage systems skip invulnerable targets; dash routes through collision resolution for wall clamping.
- Controls: Attack now LMB/J; dash on Shift/Space with 0.2s duration, 0.15s i-frames, 2s cooldown, 50ms buffer.
- Tests: `dotnet test`; Build: `dotnet build`.

## Implementation Plan

### Phase 1: Core Dash Components & Configuration
**Goal:** Establish the data model for dash behavior

**Files to Create:**
- `src/Game/Core/Ecs/Components/DashComponents.cs`
  - `DashConfig` struct: distance (150f), duration (0.2s), cooldown (2s), i-frame window (0.15s)
  - `DashState` struct: active, elapsed time, i-frame active boolean
  - `DashCooldown` struct: remaining cooldown seconds
  - `InputBuffer` struct: buffered dash input with timestamp (50ms window)
  - `Invulnerable` tag component: simple marker for i-frame state

**Files to Modify:**
- `src/Game/Core/Input/InputState.cs`
  - Add `DashPressed` boolean property
  - Map to Shift/Spacebar (Spacebar currently triggers attack, may need adjustment)
  - Add key state tracking (`_previousKeyboard`) for edge detection

**Configuration Constants:**
```csharp
// DashConfig default values
const float DefaultDistance = 150f;
const float DefaultDuration = 0.2f;     // Fast, snappy dash
const float DefaultCooldown = 2.0f;     // Encourage tactical use
const float IFrameWindow = 0.15f;       // 75% of dash duration
const float InputBufferWindow = 0.05f;  // 50ms buffer
```

**Rationale:**
- Duration 0.2s gives 750 units/sec speed (distance/duration)
- I-frames cover most of dash to allow enemy/projectile dodging
- Buffer window matches fighting game standards

---

### Phase 2: Dash Input System
**Goal:** Detect dash input, buffer it, and gate by cooldown

**Files to Create:**
- `src/Game/Core/Ecs/Systems/DashInputSystem.cs`
  - Process dash button press from `InputState`
  - Check `DashCooldown` component; reject if on cooldown
  - Buffer input if player is in hit-stun or attacking
  - Publish `DashRequestEvent` when valid
  - Update/decay input buffer each frame

**Files to Modify:**
- `src/Game/Core/Events/GameplayEvents.cs`
  - Add `DashRequestEvent(Entity actor, Vector2 direction)`

**System Integration:**
- Insert after `InputSystem` (reads same input state)
- Before movement/combat systems (dash overrides intent)

**Buffering Logic:**
```csharp
// If dash pressed but currently unable:
if (isHitStunned || isAttacking) {
    inputBuffer.Timestamp = currentTime;
    inputBuffer.HasInput = true;
}
// Later, when able:
if (inputBuffer.HasInput && (currentTime - inputBuffer.Timestamp) < 0.05f) {
    // Consume buffered input
}
```

---

### Phase 3: Dash Execution System
**Goal:** Apply dash movement and enable i-frames

**Files to Create:**
- `src/Game/Core/Ecs/Systems/DashExecutionSystem.cs`
  - Subscribe to `DashRequestEvent`
  - Calculate dash direction from player facing/input
  - Set `DashState` component (active, elapsed=0, i-frame=true)
  - Apply initial velocity override (direction × speed)
  - Add `Invulnerable` tag component
  - Start cooldown timer
  - Trigger dash start VFX/SFX via event bus

**Dash Direction Logic:**
```csharp
// Priority: input direction > velocity direction > facing direction
Vector2 dashDirection;
if (inputIntent.Movement.LengthSquared() > 0.01f) {
    dashDirection = Vector2.Normalize(inputIntent.Movement);
} else if (velocity.Value.LengthSquared() > 0.01f) {
    dashDirection = Vector2.Normalize(velocity.Value);
} else {
    // Use PlayerAnimationState.Facing converted to vector
    dashDirection = FacingToVector(animState.Facing);
}
```

**Integration Point:**
- Runs after `DashInputSystem`
- Before `MovementIntentSystem` (overrides velocity)

---

### Phase 4: Dash Movement & Update System
**Goal:** Animate dash over time, manage i-frame window, handle end

**Files to Create:**
- `src/Game/Core/Ecs/Systems/DashMovementSystem.cs`
  - Update `DashState.Elapsed` each frame
  - Calculate dash speed curve (linear or ease-out)
  - Apply velocity override during dash
  - Toggle `Invulnerable` at i-frame end (e.g., 0.15s)
  - Remove `DashState` and `Invulnerable` when elapsed >= duration
  - Trigger dash end VFX/SFX

**Speed Curve Options:**
- **Linear:** constant speed throughout
- **Ease-out:** fast start, slow end (progress³)
- **Recommendation:** Linear for responsiveness

**System Order:**
```
DashExecutionSystem (start)
  ↓
DashMovementSystem (update)
  ↓
CollisionResolutionSystem (apply)
```

**I-Frame Management:**
```csharp
// Toggle invulnerability mid-dash
if (dashState.Elapsed > config.IFrameWindow && world.HasComponent<Invulnerable>(entity)) {
    world.RemoveComponent<Invulnerable>(entity);
}
```

---

### Phase 5: Collision Integration
**Goal:** Allow enemy/projectile passthrough during i-frames, respect walls

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/MeleeHitSystem.cs`
  - Check for `Invulnerable` tag before applying damage
  - Skip hit if present
  
- `src/Game/Core/Ecs/Systems/ProjectileHitSystem.cs`
  - Same invulnerability check
  
- `src/Game/Core/Ecs/Systems/ContactDamageSystem.cs`
  - Check invulnerability before contact damage

- `src/Game/Core/Ecs/Systems/Collision/CollisionSystem.cs`
  - **No changes needed** for trigger collisions (still emit events)
  - Damage systems handle invulnerability filtering

- `src/Game/Core/Ecs/Systems/CollisionResolutionSystem.cs`
  - **Critical:** Dash velocity must respect static world colliders
  - Existing system already handles this (runs after velocity set)
  - Ensure dash doesn't tunnel: clamp to MaxKnockbackSpeed pattern

**Wall Clamping:**
```csharp
// In DashMovementSystem, before setting velocity:
var intendedPosition = position.Value + dashVelocity * deltaTime;
var contact = TestCollisionAtPosition(intendedPosition);
if (contact.IsColliding) {
    // Slide along wall (project velocity onto tangent)
    var dot = Vector2.Dot(dashVelocity, contact.Normal);
    if (dot < 0) {
        dashVelocity -= contact.Normal * dot;
    }
}
```

---

### Phase 6: Camera Nudge & Feedback
**Goal:** Add visual/audio polish and telegraph cues

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/HitStopSystem.cs`
  - Reuse camera shake system for dash nudge
  - Add `TriggerCameraNudge(float intensity, float duration)` method
  - Intensity ~2-3 pixels, duration 0.1s
  - Respect `EnableCameraShake` toggle (accessibility)

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/VfxSystem.cs`
  - Add dash trail/afterimage VFX type
  - Spawn at dash start position, fade over 0.3s

- `src/Game/Core/Ecs/Systems/SfxSystem.cs`
  - Add "DashStart" and "DashEnd" sound effect hooks

**Dash Events:**
```csharp
// In DashExecutionSystem (start):
world.EventBus.Publish(new VfxSpawnEvent("dash_start", position, VfxType.DashTrail));
world.EventBus.Publish(new SfxPlayEvent("DashWhoosh", SfxCategory.Movement, position));
hitStopSystem.TriggerCameraNudge(2.5f, 0.1f);

// In DashMovementSystem (end):
world.EventBus.Publish(new VfxSpawnEvent("dash_end", position, VfxType.Impact));
world.EventBus.Publish(new SfxPlayEvent("DashStop", SfxCategory.Movement, position));
```

---

### Phase 7: UI Feedback
**Goal:** Show cooldown and dash availability in HUD

**Files to Create:**
- `src/Game/Core/Ecs/Systems/DashUiSystem.cs`
  - Render dash cooldown indicator in screen space
  - Position: bottom-right corner (opposite to health bar)
  - Visual: circular progress bar or bar fill
  - Color: Blue when available, gray when on cooldown
  - Show input buffer icon when buffered (yellow flash)

**Alternatively, Extend Existing:**
- `src/Game/Core/Ecs/Systems/HudRenderSystem.cs`
  - Add `DrawDashCooldown()` method
  - Query player for `DashCooldown` component
  - Render at fixed screen position (e.g., 880, 480)

**UI Layout:**
```
┌────────────────────────┐
│  Wave 5   00:45   123  │  ← Existing HUD
│                        │
│  [Player]              │
│   ████░ HP             │
│                    [D] │  ← Dash indicator
└────────────────────────┘
```

---

### Phase 8: Debug Overlay & Toggles
**Goal:** Allow testing i-frame timing and dash behavior

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/DebugInputSystem.cs`
  - Add F9 toggle for dash debug visualization
  - Show i-frame window as overlay on player
  - Display cooldown timer as text

- `src/Game/Core/Ecs/Systems/CollisionDebugRenderSystem.cs`
  - Add i-frame visualization (pulsing outline)
  - Show dash trajectory line (intended path)
  - Render when debug mode active

**Debug Overlay:**
```csharp
// In CollisionDebugRenderSystem.Draw():
if (world.HasComponent<Invulnerable>(entity)) {
    // Draw pulsing cyan outline
    DrawCircle(position, radius + 2, Color.Cyan * pulseAlpha);
}
if (world.HasComponent<DashState>(entity)) {
    // Draw dash trajectory
    var endPos = position + dashDirection * remainingDistance;
    DrawLine(position, endPos, Color.Yellow);
}
```

---

### Phase 9: Testing
**Goal:** Ensure correctness and prevent regressions

**Files to Create:**
- `src/Game.Tests/Movement/DashSystemTests.cs`
  - `Dash_CooldownGating_RejectsDashWhenOnCooldown`
  - `Dash_IFrames_IgnoresEnemyDamage`
  - `Dash_IFrames_RespectProjectileDamage`
  - `Dash_WallCollision_ClampsEndPosition`
  - `Dash_InputBuffer_ConsumesBufferedInput`
  - `Dash_Direction_PrioritizesInputOverFacing`
  - `Dash_Cooldown_DecrementsOverTime`

**Test Patterns:**
```csharp
[Fact]
public void Dash_IFrames_IgnoresEnemyDamage() {
    // Arrange: player with Invulnerable tag
    var player = CreatePlayer(world);
    world.SetComponent(player, new Invulnerable());
    
    // Act: trigger melee hit
    var damage = ApplyMeleeHit(player);
    
    // Assert: health unchanged
    var health = world.GetComponent<Health>(player);
    Assert.Equal(100f, health.Current);
}
```

---

### Phase 10: Documentation & Polish
**Goal:** Update docs and ensure build passes

**Files to Modify:**
- `docs/game-design-document.md`
  - Add "Dash Mechanics" subsection under Player Combat
  - Document cooldown, i-frames, input buffering
  - Add to controls reference

- `README.md` or `CONTROLS.md` (if exists)
  - Add Shift/Spacebar for dash (update keybindings)

**Files to Create:**
- `docs/design/033-dash-defense-moves-implementation.md`
  - Architecture overview (components, systems, events)
  - System order diagram
  - I-frame timing chart
  - Cooldown/buffering logic flowchart
  - Test coverage summary

**Build & Run Check:**
```bash
dotnet build
dotnet test
dotnet run --project src/Game
# Manual test: dash into walls, through enemies, during cooldown
```

---

## System Integration Order

Updated ECS pipeline with dash systems:

```
1. InputSystem                    ← Read keyboard/gamepad
2. DashInputSystem                ← NEW: Buffer dash input, check cooldown
3. DashExecutionSystem            ← NEW: Start dash on valid request
4. DashMovementSystem             ← NEW: Update dash state, apply velocity
5. MovementIntentSystem           ← Sets velocity (overridden by dash)
6. KnockbackSystem
7. CollisionResolutionSystem      ← Clamps dash against walls
8. CollisionSystem                ← Detects collisions, emits events
9. DynamicSeparationSystem
10. ContactDamageSystem           ← Check Invulnerable tag
11. MeleeHitSystem                ← Check Invulnerable tag
12. ProjectileHitSystem           ← Check Invulnerable tag
13. AnimationEventSystem
14. CombatSystem
15. HitStopSystem                 ← Camera nudge on dash
16. VfxSystem                     ← Dash trail/end effects
17. SfxSystem                     ← Dash sounds
18. MovementSystem
```

---

## Key Design Decisions

### Cooldown vs Stamina
**Decision:** Use cooldown-based gating (simpler, aligns with skill system)
**Rationale:** 
- Stamina adds UI complexity (bar management)
- Cooldown matches skill hotbar patterns
- Can add stamina later if needed (Task 031+)

### Input Mapping
**Decision:** Map dash to Shift (primary), Spacebar (alt)
**Note:** Spacebar currently triggers attack; may need to differentiate (Shift+Space?)
**Alternative:** Use Right-Click for dash, Left-Click for attack

### I-Frame Duration
**Decision:** 0.15s of 0.2s dash (75%)
**Rationale:**
- Covers most of dash movement
- Ends slightly before dash completes (prevents abuse)
- Aligns with enemy attack timings (~0.35s melee)

### Collision Passthrough
**Decision:** Keep collision events, filter damage in combat systems
**Rationale:**
- Cleaner than modifying collision layer masks mid-dash
- Allows trigger interactions (pickup XP orbs during dash)
- Enemies still emit collision events (for AI/feedback)

### Camera Nudge
**Decision:** Reuse HitStopSystem shake, 2-3px intensity
**Rationale:**
- Existing system handles accessibility toggle
- Subtle feedback without motion sickness
- Distinct from hit shake (lower intensity, shorter duration)

---

## Risk Mitigation

### Tunneling Through Walls
**Risk:** High-speed dash bypasses collision detection
**Mitigation:**
- Dash speed capped at 750 units/sec (< MaxKnockbackSpeed 800)
- CollisionResolutionSystem runs after dash velocity set
- Additional check: swept collision test if needed

### Dash Chaining Exploits
**Risk:** Cooldown reset glitches or buffer abuse
**Mitigation:**
- Cooldown starts immediately on dash (not on end)
- Buffer consumes input (can't chain)
- Test: rapid dash button presses should respect cooldown

### I-Frame Timing Drift
**Risk:** Animation FPS vs fixed timestep mismatch
**Mitigation:**
- I-frames driven by elapsed time, not animation frames
- Fixed timestep (60 FPS) ensures deterministic timing
- Test: measure actual i-frame duration across multiple dashes

### Camera Nudge Sickness
**Risk:** Motion-sensitive players experience discomfort
**Mitigation:**
- Respect existing `EnableCameraShake` toggle (F5)
- Lower intensity than hit shake (2-3px vs 8px max)
- Accessibility note in settings/docs

---

## Open Questions

1. **Dash Animation:** Do we have/need a dash animation sprite?
   - **Fallback:** Reuse run animation with speed multiplier
   - **Future:** Add dedicated 3-frame dash sprite

2. **Dash Direction Lock:** Should direction lock at start or update?
   - **Recommendation:** Lock at start (predictable, avoids steering exploits)

3. **Cooldown Display:** Bar, circular, or numeric?
   - **Recommendation:** Circular (matches potential skill hotbar style)

4. **Multiple Dash Inputs:** Queue or ignore?
   - **Recommendation:** Ignore (buffer only holds one input)

5. **Dash Cancel:** Can player attack/cast mid-dash?
   - **Recommendation:** No (dash completes before other actions)

---

## Success Criteria Checklist

- [ ] Player can dash with Shift/Spacebar input
- [ ] Dash has 2s cooldown, visualized in UI
- [ ] I-frames active for 0.15s, enemies/projectiles don't damage
- [ ] Dash respects walls (no tunneling), slides along surfaces
- [ ] Input buffering works (50ms window)
- [ ] Camera nudges subtly on dash start
- [ ] VFX trail and SFX play on dash start/end
- [ ] Debug overlay (F9) shows i-frame window and trajectory
- [ ] All tests pass (7+ test cases)
- [ ] `dotnet build` succeeds
- [ ] Manual playtest: dash through enemy wave, into walls, during combat

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; dash tuning should align with mage pacing and readability.
- Ensure deterministic i-frame timing at fixed timestep; avoid drift between animation speed and logic.
- Prevent dash chaining exploits by enforcing cooldown/stamina and buffering rules.
- Sliding along walls must not overshoot; reuse collision resolution to clamp displacement.
- Camera nudge should be subtle; respect existing shake limits (Task 028).
- Consider accessibility: allow disabling camera nudge while keeping hit-stop.***

