# Camera System Specification (Phase 4)

## Overview
The camera system provides a high-speed arcade third-person chase perspective designed for mobile screens. It follows the player's vehicle smoothly, eliminates rigid physics-jitter, leans into high-speed turns, and acts as the centralized screen-space feedback receiver for the entire combat racing game.

## Design Pillars Served
- **Pillar 1: Readable Chaos**
  Combat and racing create high visual complexity. The camera maintains a stable, smoothed vantage point (`Vector3.SmoothDamp`) behind the vehicle while dynamically expanding field of view and applying localized screen shake to communicate impact intensity without causing motion sickness.
- **Pillar 3: One-Thumb Mastery**
  Dynamic look-ahead anticipates apex turning angles, giving the player immediate visual cues of the upcoming track curvature and apex even before manual steering adjustments are fully completed.

## Architecture & Components

### 1. `ThirdPersonCameraController.cs`
Mounted directly on the Main Camera (`ACR.Core` assembly).
- **Smooth Follow**: Uses `Vector3.SmoothDamp` on vehicle offset position rather than raw rigid parenting or naive `Lerp`. This prevents suspension vibrations and sudden collision impulses from jarring the camera.
- **Look-Ahead Lean**: Tracks the vehicle's yaw rotation delta per frame (`Vector3.SignedAngle`). When the car initiates a turn, the look-at point shifts dynamically towards the apex corner (`target.right * currentLookAhead * lookAheadAmount`).
- **Screen Shake Engine**: `TriggerShake(float intensity, float duration)` generates decaying spherical random offsets (`Random.insideUnitSphere * intensity * falloff`). Unperturbed follow coordinates are maintained independently so shake never pollutes `followVelocity`.
- **FOV Kick Engine**: `TriggerFOVKick(float amount, float duration)` expands field of view on high-velocity events (nitro activation, boost pads) with smooth exponential relaxation back to `baseFOV`.
- **Decoupled Architecture**: Does not reference vehicle physics internals. Any transform can be targeted (`SetTarget(Transform)`), allowing seamless transitions during character selection, garage view, and spectator modes.

### 2. Tuning Parameters
| Parameter | Default | Purpose |
|---|---|---|
| `followDistance` | `8.0f` | Distance behind the car along the reverse forward vector. |
| `followHeight` | `3.5f` | Elevation above the car for clear track visibility over the rear wing. |
| `positionSmoothTime` | `0.15f` | SmoothDamp latency (smaller = snappier, larger = floatier). |
| `rotationSmoothSpeed` | `6.0f` | Slerp rate towards the look-at target. |
| `lookAheadAmount` | `1.5f` | Multiplier for how far sideways the camera looks into corners. |
| `lookAheadSmoothSpeed`| `4.0f` | Smoothing rate for corner anticipation. |
| `baseFOV` | `60.0f` | Standard mobile FOV perspective. |

## External Integration Points (Roadmap Forward-Compatibility)
- **Phase 9 (Nitro Boost)**: Calls `ThirdPersonCameraController.Instance.TriggerFOVKick(12f, 0.4f)` on activation + subtle continuous shake `(0.1f, 0.1f)`.
- **Phase 10 & 18 (Weapons & Impacts)**: Projectile explosions and vehicle-on-vehicle rams call `ThirdPersonCameraController.Instance.TriggerShake(0.5f, 0.35f)`.
- **Phase 14 (Vehicle Selection)**: Invokes `SetTarget(garageCarTransform)` and `SnapToTarget()` to frame showroom vehicles.
