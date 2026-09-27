# Animal Combat Racing — Vehicle System Specification

**Status:** IMPLEMENTED (Phase 2 Milestone)  
**Physics Model:** Simplified Arcade Movement (4-Point Raycast Suspension + Contact-Point Forces)  
**WheelCollider Usage:** NONE (Strictly Prohibited)  

---

## 1. Architecture Overview
The vehicle system is strictly decoupled into a data-driven model and a modular physics worker:

```
[VehicleDataSO] (ScriptableObject Asset)
      │
      │ (Configures mass, topSpeed, accel, suspension, grip, nitro)
      ▼
[VehicleController] (MonoBehaviour on Rigidbody)
      │
      ├── 4x Wheel Sockets (FL, FR, RL, RR)
      │     └── Physics.Raycast(wheel.position, -transform.up, maxRayLength)
      │
      ├── Suspension Calculation (Spring Force + Damper Force at contact)
      ├── Drive Force (Applied at grounded wheel positions)
      └── Lateral Grip Force (Cancels perpendicular sliding velocity)
```

### Design Pillars Served:
* **Pillar 3 (*One-Thumb Mastery*)**: 
  * `highSpeedSteerRetention` (0.4) reduces max steering sensitivity as speed increases, preventing twitchy over-steering at top speed.
  * Deterministic raycast physics ensures predictable handling curves.
* **Pillar 4 (*Personality Over Realism*)**: 
  * Spring stiffness and damping produce bouncy cartoon suspension without tipping over.
  * Low center of mass (`rb.centerOfMass = new Vector3(0, -0.5f, 0)`) prevents rollovers on aggressive turns.

---

## 2. Component API Reference

### `VehicleDataSO` (`Assets/_Project/Scripts/Vehicle/VehicleDataSO.cs`)
* `topSpeed` (float): Forward speed clamp (units/sec).
* `accelerationForce` (float): Forward force applied per FixedUpdate during throttle.
* `brakeForce` (float): Reverse force applied when braking.
* `coastingDrag` (float): Passive rolling resistance to stop runaway rolling.
* `maxSteerAngle` (float): Maximum wheel angle at low speed (degrees).
* `steerSpeed` (float): Rate of steering angle interpolation.
* `highSpeedSteerRetention` (float): Percentage of steer angle retained at max speed (0.0 to 1.0).
* `suspensionRestDistance` (float): Resting spring distance from wheel socket (meters).
* `springStrength` (float): Spring stiffness coefficient.
* `springDamper` (float): Damper coefficient to prevent bouncing oscillations.
* `wheelRadius` (float): Wheel radius added to raycast length.
* `gripFactor` (float): Fraction of lateral sliding velocity cancelled at the tire contact patch (0.0 to 1.0).
* `mass` (float): Rigidbody mass (kg).

### `VehicleController` (`Assets/_Project/Scripts/Vehicle/VehicleController.cs`)
* `WheelGrounded` (bool[]): Read-only array reporting ground contact per wheel (FL, FR, RL, RR).
* `VehicleData` (VehicleDataSO): Public reference to vehicle stat asset.

---

## 3. Phase 2 Scope Boundaries

* **In Scope**:
  * 4-point raycast suspension (ride height, spring force, damping).
  * Acceleration, braking, coasting drag, and lateral grip cancellation at wheel contact points.
  * Temporary debug input (`Vertical`, `Horizontal`, `Space`) for suspension and handling testing.
* **Deferred to Later Phases**:
  * Production Input System (`VehicleInput`) $\rightarrow$ **Phase 3**.
  * Dynamic Camera $\rightarrow$ **Phase 4**.
  * Nitro logic $\rightarrow$ **Phase 9**.
  * Weapons & Health $\rightarrow$ **Phases 10 & 12**.
