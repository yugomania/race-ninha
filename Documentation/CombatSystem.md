# Animal Combat Racing — Combat System Specification (Phase 10)

## Overview
The Combat System provides dynamic, arcade vehicular warfare. To prevent mobile garbage collection spikes and micro-stutter during intense multi-vehicle firefights, **zero runtime allocations (`ObjectPool<T>`)** are mandated from the very first weapon.

## Design Pillars Served
- **Pillar 1: Readable Chaos**
  Weapons have distinct visual trajectories and limited turn rates. The homing missile does not snap instantaneously to targets; it carves an observable arc, allowing skilled players to dodge or use terrain/obstacles for cover.
- **Pillar 2: Always a Comeback**
  Trailing karts can acquire and launch homing weapons against leaders, directly supporting rubber-banding pack dynamics.

---

## Architecture & Modular Components

```
                   ┌───────────────────────┐
                   │     ObjectPool<T>     │  (ACR.Core: Zero-GC prewarmed queue)
                   └───────────▲───────────┘
                               │
                   ┌───────────┴───────────┐
                   │      WeaponBase       │  (ACR.Combat: Cooldowns & Controls Lock)
                   └───────────▲───────────┘
                               │
               ┌───────────────┴───────────────┐
               │                               │
    ┌──────────┴──────────┐        ┌───────────┴───────────┐
    │ HomingMissileWeapon │        │ (Phase 18 Weapons)    │
    └──────────┬──────────┘        └───────────────────────┘
               │ Launches
               ▼
    ┌──────────────────────┐
    │HomingMissileProjectil│  (Curves at 120°/s towards nearest vehicle)
    └──────────────────────┘
```

### 1. `ObjectPool<T>` (`ACR.Core`)
- Generic pool `where T : Component`.
- Prewarms instances during scene staging or weapon initialization (`available.Enqueue`).
- `Get()` retrieves an existing inactive instance or instantiates on-demand if exhausted.
- `Release(instance)` disables the GameObject and enqueues it back to the pool.
- Eliminates `Instantiate` and `Destroy` during active racing.

### 2. `WeaponBase` (`ACR.Combat`)
- Abstract base class for all vehicle weapons.
- Automatically connects to `VehicleController.InputSource` in `Start()`.
- Checks `inputSource.WeaponFireRequested` and respects `!vehicleController.ControlsLocked`.
- Enforces `cooldownDuration` (2.0s) before delegating to `abstract void Fire(VehicleController firer)`.

### 3. `HomingMissileWeapon` & `HomingMissileProjectile`
- **Target Acquisition**: Searches for the nearest `VehicleController` within `targetSearchRadius = 40f`. If no target is present, the missile fires straight forward.
- **Arcade Steering**:
  ```csharp
  Vector3 newDirection = Vector3.RotateTowards(
      transform.forward, desiredDirection,
      turnRateDegreesPerSec * Mathf.Deg2Rad * Time.deltaTime, 0f);
  ```
  Turn rate is clamped to `120°/sec`, creating a readable, escapable homing arc.
- **Impact & Return to Pool**:
  On `OnTriggerEnter`, ignores the firer. When striking another vehicle, it logs `[Phase10 stub]` (real damage arrives in Phase 12) and invokes `returnToPool(this)` to recycle the missile without memory allocation.

---

## Tuning Parameters
| Parameter | Default | Purpose |
|---|---|---|
| `cooldownDuration` | `2.0f` | Seconds between consecutive weapon shots. |
| `missileSpeed` | `30.0f` | Projectile forward velocity (units/sec). |
| `missileTurnRateDegreesPerSec` | `120.0f` | Maximum homing angular turn rate (degrees/sec). |
| `missileLifetime` | `4.0f` | Maximum flight time before automatic despawn / pool return. |
| `targetSearchRadius` | `40.0f` | Maximum distance to acquire a lock on an opponent vehicle. |
| `poolPrewarmCount` | `5` | Initial instances allocated to the object pool. |
