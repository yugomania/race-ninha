# Animal Combat Racing — Vehicle System Specification

**Status:** IMPLEMENTED (Phase 9 Milestone: Nitro Boost System & Physics Multiplier)  
**Physics Model:** Simplified Arcade Movement (4-Point Raycast Suspension + Contact-Point Forces)  
**WheelCollider Usage:** NONE (Strictly Prohibited)  

---

## 1. Architecture Overview: Decoupled Input & Control Pipeline

Input is abstracted via the `IVehicleInputSource` interface, ensuring zero coupling between control methods, physics, and gameplay boosters:

```
[Hardware / Input Layer]
  ├── Desktop / Editor ──────> [KeyboardVehicleInput] ──┐
  │                                                     │
  ├── Mobile Screen Zones ───> [TouchVehicleInput] ─────┼──> [IVehicleInputSource]
  │                                  ├── Auto-Accelerate│           │
  │                                  ├── Steer-Assist   │           ▼
  │                                  └── Nitro Button   │    [VehicleController]
  │                                                     │    (Raycast Suspension)
  └── AI Autonomous Driver ──> [AIController] ──────────┘           │
                                                                    ▼
                                                             [NitroSystem]
                                                             (Synchronous Tick)
```

### Design Pillars Served:
* **Pillar 2 (*Always a Comeback*)**: 
  Nitro provides rubber-banding burst acceleration, allowing trailing karts to close distance in straights and execute high-speed overtakes.
* **Pillar 3 (*One-Thumb Mastery*)**: 
  Nitro is exposed as a discrete toggle/button (`SetNitroButtonHeld`) on touch, complementing one-thumb auto-accelerate and steer assist.

---

## 2. Component API Reference

### `IVehicleInputSource` (`Assets/_Project/Scripts/Vehicle/IVehicleInputSource.cs`)
```csharp
public interface IVehicleInputSource
{
    float Throttle { get; }       // -1 (reverse) to +1 (forward)
    float Steer { get; }          // -1 (left) to +1 (right)
    bool Brake { get; }           // True when handbrake is active
    bool NitroRequested { get; }  // True while nitro boost input is held
}
```

### `NitroSystem` (`Assets/_Project/Scripts/Vehicle/NitroSystem.cs`)
* **Synchronous Execution**: Has no independent `FixedUpdate` or `Update`. It is called directly from `VehicleController.FixedUpdate` via `bool Tick(bool requested, float deltaTime)`:
  - Eliminates 1-frame timing discrepancies or component execution order lag.
  - Returns `isBoosting` state in the exact physics step where drive forces are applied.
* **Depletion & Recharge**:
  - Depletion: $\text{CurrentNitro} = \max(0, \text{CurrentNitro} - \text{nitroDepletionRate} \times \Delta t)$
  - Recharge: $\text{CurrentNitro} = \min(\text{MaxNitro}, \text{CurrentNitro} + \text{nitroRechargeRate} \times \Delta t)$
* **Screen-Space Feedback**:
  - On the rising edge (`isBoosting && !wasBoosting`), triggers `cameraController.TriggerFOVKick(8f, 0.4f)` using the centralized Phase 4 camera hook.

### `VehicleController` Boost Physics
When boosting:
$$\text{effectiveTopSpeed} = \text{vehicleData.topSpeed} \times \text{vehicleData.nitroBoostMultiplier}$$
$$\text{effectiveAccelerationForce} = \text{vehicleData.accelerationForce} \times \text{vehicleData.nitroBoostMultiplier}$$
* While `controlsLocked == true` (Countdown Staging), nitro is strictly suppressed (`nitroRequested = false`).

### Data Tunables (`VehicleDataSO.cs`)
* `nitroCapacity`: Default `100.0f`
* `nitroRechargeRate`: Default `8.0f` units/sec
* `nitroDepletionRate`: Default `25.0f` units/sec (4 seconds of continuous burn)
* `nitroBoostMultiplier`: Default `1.4f` (+40% top speed and acceleration force)
