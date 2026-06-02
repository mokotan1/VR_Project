# Enemy Attack Design

## One-Line Concept

SUPERHOT-style asymmetric instant-kill combat: player and enemies both die in one valid hit, with telegraphed enemy attacks that are dodgeable (melee) or coverable (ranged).

## Combat Asymmetry

| | Player | Enemy |
|---|--------|-------|
| Valid hit | Instant kill (`SuperhotEnemy.Kill`) | Instant kill (`SuperhotPlaytestPlayerHealth.ApplyHit`) |
| Advantage | Time smoothing (movement drives time) | Numbers, flanking, range |
| Defense (melee) | Dodge + parry/shield | — |
| Defense (ranged) | Cover / aim-break | — |

Crystal defense is **out of scope** — enemies target the player only.

## Attack State Machine

All melee and ranged attacks share:

```text
Idle → WindUp → Active → Recovery → Idle
```

| Phase | Melee | Ranged |
|-------|-------|--------|
| WindUp | 0.4–0.7s arm/body telegraph, SFX, VR glow | 0.5–0.8s aim, laser/sight, body stops |
| Active | 0.12–0.18s forward arc hitbox only | Single shot or slow projectile |
| Recovery | 0.6–1.0s counter-kill window | Resume strafe, cooldown |

**Forbidden:** off-screen hits, instant hitscan without aim telegraph, multiple simultaneous active melee hitboxes (Rusher cap: 1).

## Defense Rules (Option C)

**Melee**

- **Dodge:** survive if outside active hitbox during Active phase.
- **Parry/Shield:** block during active window → cancel hit, stun enemy 1–2s.
- **Failure:** inside hitbox without block → instant death.

**Ranged** (later phase)

- **Cover:** blocks LOS during aim → cancel or reset aim.
- **No dodge** — only cover/aim-break.
- **Failure:** aim completes and shot connects → instant death.

## AI Mapping

| `SuperhotEnemyBrain` state | Behavior |
|----------------------------|----------|
| Engaging | Shooter: strafe + aim/fire (later). Melee: approach only. |
| CloseRange | Begin telegraphed melee when in range. |
| Flank / Investigate | No attacks. |

**Removed:** `CloseRange` speed debuff (unfair with 1HK), crystal objective/attack branches.

## Enemy Archetypes

| Type | WindUp | Pattern | Counter |
|------|--------|---------|---------|
| Stalker (default) | 0.6s | Single strike | Dodge + parry |
| Shooter | 0.7s aim | Strafe + single shot | Cover |
| Rusher | 0.25s | Double tap | Dodge; max 1 active globally |
| Bruiser | 0.8s | Heavy strike + knockback | Long stun on parry |

## Time Smoothing

- WindUp affected by SUPERHOT time scale → readable telegraph when player moves slowly.
- Active = commit window — must dodge or block.
- Panic movement speeds time → shorter telegraph (risk/reward).

## Player Health

- `SuperhotPlaytestPlayerHealth`: **1 hit = defeat** (default `_startingHits = 1`).
