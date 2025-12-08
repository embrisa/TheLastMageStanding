# Task 044 — Skill Balance & Feel Pass

**Status:** backlog  
**Priority:** High  
**Estimated effort:** 2-3 hours (playtesting + iteration)  
**Dependencies:** Task 040-043 (full skill UI/input/progression)

## Summary

Playtest all 9 mage skills in real gameplay scenarios and tune damage, cooldowns, cast times, and visual/audio feedback for satisfying moment-to-moment combat feel.

## Rationale

Current skill values are placeholder estimates. Real gameplay will reveal balance issues (too weak/strong), feel problems (unresponsive, awkward), and clarity issues (visual/audio feedback). This pass ensures skills feel good to use before building more systems on top.

## Goals

- Playtest each skill against waves of enemies at various levels
- Tune damage multipliers, cooldowns, and cast times for balanced power budget
- Adjust projectile speeds, AoE radii, and hitbox sizes for usability
- Add/refine VFX and SFX for satisfying impact feedback
- Document final tuned values in design doc
- Create baseline skill DPS calculations for future balance reference

## Non-Goals

- Major mechanical changes (new skill types, behaviors)
- Additional skills beyond the 9 mage skills
- Status effects/CC (Task 034 handles frost slow, burn DoT, etc.)
- Advanced combos/synergies (future)
- Skill modifiers from talents/equipment (works with existing values)

## Acceptance Criteria

1. All 9 skills feel responsive and impactful in real combat
2. No skill is obviously overpowered or useless
3. Skill power scales appropriately with player level/stats
4. Cooldowns create meaningful choice (not spamming one skill)
5. Cast times feel fair (telegraphed but not sluggish)
6. VFX clearly communicate skill element and impact
7. SFX provide satisfying audio feedback on cast and hit
8. Documented balance framework for future skill additions

## Plan

### 1. Establish balance baseline
**Reference DPS calculation:**
```
Skill DPS = (baseDamage × power × damageMultiplier × 10) / effectiveCooldown
Effective Cooldown = baseCooldown × (1 / attackSpeed)
```

**Example (Firebolt at level 1):**
- Base damage: 20 (player power 1.0)
- Damage multiplier: 1.0
- Cooldown: 0.5s
- DPS = (20 × 1.0 × 1.0 × 10) / 0.5 = 400 DPS

**Target power budget (level 1):**
- Fast single-target: ~400 DPS (Firebolt baseline)
- AoE burst: ~300 DPS per target (higher total, longer CD)
- Utility/CC: ~250 DPS (power in crowd control, not damage)

### 2. Playtesting methodology
**Test scenarios:**
1. **Wave 1-3 (early game):** Player level 1-3, basic stats
   - Test starter skills (Firebolt, Frost Bolt)
   - Verify low-level enemies die in 2-3 hits
   - Check cooldowns feel responsive

2. **Wave 5-7 (mid game):** Player level 5-7, some perks/equipment
   - Test AoE skills (Fireball, Arcane Burst)
   - Verify multi-target clearing feels efficient
   - Check cast times don't leave player vulnerable

3. **Wave 10+ (late game):** Player level 10+, optimized build
   - Test ultimate skills (Arcane Barrage, Blizzard)
   - Verify skills scale with power/crit/CDR
   - Check high-pressure situations (40+ enemies)

**Metrics to track:**
- Time to kill basic enemy (target: 1-2 seconds)
- Skill usage frequency (are players using all equipped skills?)
- Deaths during cast times (are channels too risky?)
- Cooldown downtime (are players waiting too long?)

### 3. Skill-by-skill review

#### Fire Skills

**Firebolt (baseline fast projectile):**
- Current: 0.5s CD, 1.0× dmg, instant, 500 speed
- **Test:** Does it feel responsive? Is damage sufficient for trash clearing?
- **Tune:** If too weak, increase speed to 600 or reduce CD to 0.4s

**Fireball (AoE burst):**
- Current: 2s CD, 3.5× dmg, 60 radius, 0.3s cast, 300 speed
- **Test:** Does AoE hit multiple targets reliably? Is cast time acceptable?
- **Tune:** If AoE feels small, increase radius to 80. If too slow, reduce cast to 0.2s

**Flame Wave (self-centered burst):**
- Current: 5s CD, 2.0× dmg, 150 radius, 0.2s cast
- **Test:** Does it feel like a strong defensive/escape tool? Is CD too long?
- **Tune:** If underused, reduce CD to 4s or increase radius to 180

#### Arcane Skills

**Arcane Missile (homing projectile):**
- Current: 0.8s CD, 1.2× dmg, 450 speed
- **Test:** Does homing work? Is it worth the longer CD vs Firebolt?
- **Tune:** If underpowered, increase dmg to 1.4× or reduce CD to 0.7s
- **Note:** Homing not implemented yet; test as fast projectile for now

**Arcane Burst (defensive AoE):**
- Current: 3s CD, 2.5× dmg, 100 radius, 0.15s cast
- **Test:** Does it feel like a panic button? Is radius large enough?
- **Tune:** If too weak, increase radius to 120 or reduce cast to instant

**Arcane Barrage (multi-shot):**
- Current: 4s CD, 5× 0.8dmg (4.0× total), 500 speed, 0.5s cast
- **Test:** Does multi-shot spread hit multiple targets? Is cast time fair?
- **Tune:** If spread too narrow, increase to 40°. If too slow, reduce cast to 0.3s

#### Frost Skills

**Frost Bolt (chill projectile):**
- Current: 0.6s CD, 0.9× dmg, 450 speed
- **Test:** Does lower damage feel acceptable for utility? (chill not implemented yet)
- **Tune:** If too weak, increase dmg to 1.0× or reduce CD to 0.5s
- **Note:** Chill CC is Task 034; for now, test pure damage

**Frost Nova (freeze burst):**
- Current: 8s CD, 1.5× dmg, 120 radius, 0.1s cast
- **Test:** Does long CD feel justified? Is radius adequate?
- **Tune:** If underused, reduce CD to 6s or increase radius to 150
- **Note:** Freeze CC is Task 034; test damage/feel for now

**Blizzard (ground AoE):**
- Current: 10s CD, 4.0× dmg, 150 radius, 0.4s cast
- **Test:** Does it feel like an ultimate/boss-killer? Is CD acceptable?
- **Tune:** If underwhelming, increase dmg to 5.0× or reduce CD to 8s
- **Note:** Ground targeting not implemented; test as self-cast for now

### 4. VFX/SFX feedback tuning
**Current implementation:**
- VFX: Element-colored projectiles/AoE
- SFX: Generic cast/impact sounds

**Enhancements needed:**
- Differentiate Fire (bright, explosive) vs Arcane (shimmery, arcane) vs Frost (crystalline, icy)
- Add screen shake scaling (bigger AoE = more shake)
- Add particle trails for projectiles
- Unique SFX per skill (not just element)
- Hit-stop scaling (faster skills = less hit-stop, big AoE = more hit-stop)

**Priority order:**
1. Projectile trails (high impact, low cost)
2. AoE explosion effects (critical for clarity)
3. Unique cast sounds (polish)
4. Impact particles (polish)

### 5. Document tuned values
Create balance reference table in design doc:

| Skill | Damage | CD | Cast | Speed | Radius | DPS | Notes |
|-------|--------|----|----|-------|--------|-----|-------|
| Firebolt | 1.0× | 0.5s | 0s | 500 | - | 400 | Baseline |
| Fireball | 3.5× | 2s | 0.3s | 300 | 60 | 350 | AoE burst |
| ... | ... | ... | ... | ... | ... | ... | ... |

Include power budget notes and design rationale for each skill.

### 6. Automated balance tests
**File:** `src/Game.Tests/Skills/SkillBalanceTests.cs`

```csharp
[Test]
public void SkillDamageScalesWithPower()
{
    // Verify damage formula consistency
}

[Test]
public void SkillCooldownsAreReasonable()
{
    // Verify no skill has < 0.2s or > 30s cooldown
}

[Test]
public void SkillDpsWithinRange()
{
    // Verify all skills fall within 200-600 DPS range at level 1
}
```

## Definition of Done

- [ ] All 9 skills playtested in waves 1-3, 5-7, and 10+
- [ ] Damage multipliers tuned for balanced power budget
- [ ] Cooldowns adjusted for skill rotation variety
- [ ] Cast times feel responsive and fair
- [ ] VFX clearly show skill element and impact
- [ ] SFX provide satisfying feedback
- [ ] Balance reference table added to `docs/game-design-document.md`
- [ ] 3+ automated balance tests added
- [ ] `dotnet build` succeeds
- [ ] Playtest notes documented in task file

## Playtesting Worksheet

### Firebolt
- [ ] Feels responsive (instant cast, fast projectile)
- [ ] Damage adequate for basic enemies
- [ ] Cooldown allows consistent use
- **Issues:** ___________________________
- **Tuning:** ___________________________

### Fireball
- [ ] AoE radius hits multiple enemies
- [ ] Cast time feels fair
- [ ] Damage justifies 2s cooldown
- **Issues:** ___________________________
- **Tuning:** ___________________________

_(Repeat for all 9 skills)_

### General Feel
- [ ] Skills feel distinct from each other
- [ ] No skill is "always best" choice
- [ ] Cooldowns create meaningful rotation
- [ ] VFX/SFX enhance satisfaction
- **Overall notes:** ___________________________

## Risks & Unknowns

- **Subjective feel:** Balance is art + science; may need multiple iteration passes
- **Level scaling:** Values tuned for level 1 may break at level 20 with gear/perks
- **Homing/ground target:** Can't fully test skills missing mechanics (Task 034+)
- **Multiplayer implications:** N/A for now, but future co-op affects balance

## Future Enhancements

- Dynamic difficulty scaling (skills adjust power based on wave/threat level)
- Skill synergies (Fire + Frost = steam explosion)
- Skill modifiers from talents (Fireball → Piercing Fireball)
- Legendary skill variants (Blizzard → Eternal Blizzard)
- Training dummy mode for isolated skill testing

## Notes

**Balance philosophy:** Skills should feel powerful and fun. Better to err on the side of "too strong" and tune down than "too weak" and boring. Players want to feel like a powerful mage, not a weak wizard.

**Iteration plan:**
1. First pass: Quick tuning based on obvious issues
2. Second pass: Refine based on full rotation playtesting
3. Third pass: Polish VFX/SFX for satisfaction
4. Future passes: Adjust based on player feedback

**Reference games:**
- Hades: Skills feel responsive, impactful, distinct
- Vampire Survivors: Simple numbers, big impact
- Diablo 3: Clear telegraphs, satisfying hit feedback
