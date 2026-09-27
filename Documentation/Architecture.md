# Animal Combat Racing — Technical Architecture

## 1. Assembly Definition Graph
The project is strictly partitioned to guarantee fast compilation and prevent cyclic dependencies:

```
[ACR.Core] (Independent Base)
    ▲
    ├── [ACR.Vehicle] (Depends on ACR.Core, Unity.InputSystem)
    │       ▲
    │       ├── [ACR.Combat] (Depends on ACR.Core, ACR.Vehicle)
    │       │
    │       ├── [ACR.Race] (Depends on ACR.Core, ACR.Vehicle)
    │       │       ▲
    │       │       ├── [ACR.AI] (Depends on ACR.Core, ACR.Vehicle, ACR.Race)
    │       │       │
    │       │       └── [ACR.UI] (Depends on ACR.Core, ACR.Vehicle, ACR.Race, Unity.InputSystem)
```

## 2. Decoupled Data Layer
* **ScriptableObjects**: All configurations (`VehicleDataSO`, `AnimalDataSO`, `WeaponDataSO`, `TrackDataSO`) live as immutable assets.
* **Separation of Concerns**:
  * Physics does not depend on UI.
  * AI feeds the same input contract as human touch input.
  * Weapons interact via generic interfaces (`IWeapon`, `IDamageable`).
