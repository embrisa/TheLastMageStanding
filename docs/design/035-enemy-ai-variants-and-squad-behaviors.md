# 035 - Enemy AI variants & squad behaviors

## Overview
Adds three enemy support/pressure roles powered by a lightweight state machine and role configs. Chargers lunge after a telegraph, protectors grant brief projectile shields, and buffers pulse timed stat buffs to nearby allies. Wave config gains new archetypes, weights, and role caps; debug overlay shows live AI state.

## Roles & config
- **Charger**: commit range 60–120, windup 0.4s, cooldown 3.5s, knockback 400, move speed 110. Telegraph: red circle radius 46 at lunge point; hitbox spawns ~30u forward, damage ×1.5, self tap-back.
- **Protector**: shield range 80, detection 120, duration 1.5s, cooldown 5s, blocks 1 projectile for allies; telegraph blue dome. Heavier mass (0.9) to front-line.
- **Buffer**: buff range 100, duration 4s, cooldown 6s, +30% move speed buff (non-stacking; refreshes duration), 0.5s animation lock. Telegraph green pulse.

## Behavior state machine
`Idle → Seeking → (trigger) Committing | Shielding | Buffing → Cooldown → Seeking`
- **Charger**: seeks player; when within commit range and off cooldown, telegraphs, lunges after windup, applies knockback, then cooldown. Cancels if target moves far during windup.
- **Protector**: throttled scans (~0.2s) for ally + hostile projectile; applies `ShieldActive` to allies in radius for duration/block window, then cooldown.
- **Buffer**: throttled scans (~0.5s) for allies; on trigger applies/refreshes `TimedBuff` to allies in range, locks briefly, then cooldown. Buffs mark computed stats dirty for recalculation.

## Systems & order
Update chain around AI: `AiSeekSystem → RangedAttackSystem → AiChargerSystem → AiProtectorSystem → AiBufferSystem → BuffTickSystem → MovementIntentSystem`. Buff ticks remove expired buffs; projectile hits respect `ShieldActive`.

## Waves & spawning
- New archetypes: `charger_hexer` (unlock 4, weight 0.6), `protector_hexer` (unlock 6, weight 0.4), `buffer_hexer` (unlock 6, weight 0.4). Base hexer weight trimmed to 0.8.
- Caps: max 2 chargers, 1 protector, 1 buffer per wave roll; rerolls if cap hit. Spawn spacing: protectors 250–380, buffers 240–360, chargers use default 260–420.

## Debugging
- New AI overlay toggle: **F11**. Renders role/state text above enemies and range circles (charger commit, protector shield, buffer buff). Colors: Seeking white, Committing yellow, Shielding blue, Buffing green, Cooldown gray.






