# AI Driver System Specification (Phase 8)

## Overview
The AI Driver system enables autonomous non-player vehicles to stage on the starting grid, obey countdown locks, navigate the circuit via checkpoint targets, and complete multi-lap races alongside the human player.

## Design Pillars Served
- **Pillar 1: Readable Chaos**
  AI drivers navigate using the same physical Raycast Suspension vehicles as the player. They do not cheat with fake on-rails transforms or instantaneous physics snapping, ensuring their vehicular motion reads as authentic and predictable.
- **Pillar 2: Always a Comeback**
  By racing on the identical physics model with corner slowdown heuristics, AI vehicles bunch up in hairpins and chicanes, creating natural overtaking and pack-racing dynamics.
- **Pillar 3: One-Thumb Mastery**
  AI difficulty tuning will be managed via configurable `steerSensitivity`, `baseThrottle`, and `cornerSlowdownFactor`, allowing casual-friendly competition without unpredictable spikes in difficulty.

## Architecture & Decoupling

```
                  ┌──────────────────────┐
                  │ IVehicleInputSource  │
                  └──────────▲───────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
┌────────┴────────┐ ┌────────┴────────┐ ┌────────┴────────┐
│  KeyboardInput  │ │   TouchInput    │ │   AIController  │
│ (Desktop Test)  │ │ (Mobile Assists)│ │ (Autonomous AI) │
└─────────────────┘ └─────────────────┘ └─────────────────┘
                             │
                             ▼
                  ┌──────────────────────┐
                  │  VehicleController   │
                  │ (Raycast Suspension) │
                  └──────────────────────┘
```

### 1. Zero-Change Decoupling
Because `VehicleController` depends exclusively on `IVehicleInputSource`, introducing AI required **zero modifications** to vehicle physics:
- `AIController` exposes `float Throttle`, `float Steer`, and `bool Brake`.
- Assigned to `VehicleController.inputSourceBehaviour` identically to human input components.
- Staged and locked by `RaceCountdownController.racers[]` with no special-case checks.

### 2. Navigation & Steering Logic (`AIController.cs`)
- **Target Tracking**: Reuses existing `TrackManager.Checkpoints` array.
- **Grid Offset**: Starts targeting Checkpoint 1 (since the grid sits just behind Checkpoint 0 / Finish Line).
- **Steering Calculation**:
  $$\text{angleToTarget} = \text{atan2}(localDir.x, localDir.z) \times \frac{180}{\pi}$$
  $$\text{Steer} = \text{clamp}\left(\frac{\text{angleToTarget}}{45^\circ} \times \text{steerSensitivity}, -1.0, 1.0\right)$$
- **Corner Slowdown Heuristic**: Cuts throttle proportionally to steering magnitude to prevent cars from entering corners at terminal velocity:
  $$\text{Throttle} = \text{lerp}\left(\text{baseThrottle}, \text{baseThrottle} \times (1 - \text{cornerSlowdownFactor}), |\text{Steer}|\right)$$
- **Checkpoint Synchronization**: Subscribes to `TrackCheckpoint.OnVehiclePassedCheckpoint(checkpoint, vehicle)`. When its own vehicle passes the expected checkpoint, increments `targetCheckpointIndex = (index + 1) % totalCount`.
- **Restart Synchronization**: Subscribes to `RaceCountdownController.OnStagingComplete` to reset `targetCheckpointIndex = 1`.

## Tuning Parameters
| Parameter | Default | Purpose |
|---|---|---|
| `steerSensitivity` | `1.5f` | Reaction sharpness to target checkpoint heading error. |
| `baseThrottle` | `1.0f` | Maximum straight-line driving throttle (0.0 to 1.0). |
| `cornerSlowdownFactor` | `0.4f` | Percentage of throttle reduction applied during maximum lock turning. |

## Future Expansion (Post-Phase 8)
- **Phase 8.1+**: Raycast obstacle avoidance for track barriers and kart-on-kart lateral separation.
- **Phase 11**: Power-up pickup routing and tactical weapon firing.
- **Phase 26**: Rubber-banding speed scaling based on distance to player.
