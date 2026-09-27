# Animal Combat Racing — Vehicle System Specification

## 1. Requirements
* Decoupled chassis from driver.
* Configurable physics via `VehicleDataSO`.
* 4-point raycast suspension to prevent rollovers on mobile.
* Support for drifting, dynamic grip transitions, and lateral friction damping.

## 2. Architecture
* `VehicleController`: Primary physics driver operating on standard `Rigidbody`.
* `VehicleDataSO`: ScriptableObject holding acceleration, max speed, grip, mass, and nitro stats.
* `IVehicleInput`: Common interface consumed by `VehicleController`, implemented by both `PlayerInputHandler` and `AIVehicleDriver`.
