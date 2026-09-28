# Animal Combat Racing — Vehicle System Specification

**Status:** IMPLEMENTED (Phase 3 Milestone: Vehicle Controls & Touch Assists)  
**Physics Model:** Simplified Arcade Movement (4-Point Raycast Suspension + Contact-Point Forces)  
**WheelCollider Usage:** NONE (Strictly Prohibited)  

---

## 1. Architecture Overview: Decoupled Input & Control Pipeline

In Phase 3, all hardcoded debug input was completely removed from `VehicleController`. Input is now abstracted via the `IVehicleInputSource` interface:

```
[Hardware / Input Layer]
  ├── Desktop / Editor ──────> [KeyboardVehicleInput] ──┐
  │                                                     │
  └── Mobile Screen Zones ───> [TouchVehicleInput] ─────┼──> [IVehicleInputSource]
                                 ├── Auto-Accelerate    │           │
                                 └── Steer-Assist       │           ▼
  (Future AI Driver in Phase 8) [AIVehicleDriver] ──────┘    [VehicleController]
                                                               (Physics Only)
```

### Design Pillars Served:
* **Pillar 3 (*One-Thumb Mastery*)**: 
  * `TouchVehicleInput` provides two essential mobile assists:
    1. **`autoAccelerate`**: The player does not need to hold an acceleration button; throttle is pinned to 1.0 until braking, enabling true one-thumb steering.
    2. **`steerAssist`**: Automatically and smoothly recenters steering (`steerAssistRecenterSpeed = 3f`) when the player lifts their thumb, preventing erratic fishtailing from thumb jitter.
* **Modularity**:
  * Because `VehicleController` only references `IVehicleInputSource`, AI drivers in Phase 8 will implement this exact same interface without requiring a single line of physics code to change.

---

## 2. Component API Reference

### `IVehicleInputSource` (`Assets/_Project/Scripts/Vehicle/IVehicleInputSource.cs`)
```csharp
public interface IVehicleInputSource
{
    float Throttle { get; } // -1 (reverse) to +1 (forward)
    float Steer { get; }    // -1 (left) to +1 (right)
    bool Brake { get; }     // True when handbrake is active
}
```

### `KeyboardVehicleInput` (`Assets/_Project/Scripts/Vehicle/KeyboardVehicleInput.cs`)
* Reads `Input.GetAxis("Vertical")`, `Input.GetAxis("Horizontal")`, and `Input.GetKey(KeyCode.Space)`.

### `TouchVehicleInput` (`Assets/_Project/Scripts/Vehicle/TouchVehicleInput.cs`)
* **Left Screen Half**: Steering zone via horizontal touch dragging from touch origin (`steerDragRangePixels = 200f`).
* **Right Screen Half**: Throttle touch zone (accelerates on touch down).
* `autoAccelerate` (bool): When true, throttle is constantly 1.0.
* `steerAssist` (bool): When true, steering returns smoothly to 0.0 when no active drag is detected.

### `VehicleController` (`Assets/_Project/Scripts/Vehicle/VehicleController.cs`)
* Accepts any `IVehicleInputSource` via `[SerializeField] private MonoBehaviour inputSourceBehaviour` or runtime `SetInputSource(IVehicleInputSource source)`.

---

## 3. Testing & Verification

Attach [`Phase3_TestHarness`](file:///C:/Users/PC/.gemini/antigravity/scratch/animal-combat-racing/Assets/_Project/Scripts/Vehicle/Phase3_TestHarness.cs) to an empty GameObject in any test scene.
1. **Keyboard Verification**: Drive with `W/S` and `A/D`. Observe speed clamp and wheel turning.
2. **Touch Verification**: Click `Active Source: Mobile Touch`. Drag the left half of the Game view to steer; toggle `Auto-Accelerate` and observe automatic cruising.
