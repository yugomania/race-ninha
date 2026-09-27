# Animal Combat Racing — Physics & Collision Matrix Setup

**Fixed Physics Timestep:** 0.02s (50 Hz)  
**Gravity:** $\vec{g} = (0, -18.0, 0)\text{ m/s}^2$ (Exaggerated cartoon gravity for crisp ramp landings)  

---

## 1. Project Layer Assignments

| Layer ID | Layer Name | Purpose |
| :--- | :--- | :--- |
| **8** | `Vehicle` | Dynamic kart bodies (Player & AI). |
| **9** | `Track_Ground` | Drivable road surface, curbs, bridge planks (Raycast target). |
| **10** | `Track_Wall` | Armco guardrails, tire barriers, stadium walls. |
| **11** | `Projectile` | Homing missiles, energy orbs, falling bombs. |
| **12** | `PowerUpPickup` | Floating 3D mystery balls (Triggers only). |
| **13** | `CheckpointTrigger`| Lap tracking gate triggers (Triggers only). |

---

## 2. 2D Layer Collision Matrix

| Layer | Vehicle | Track_Ground | Track_Wall | Projectile | PowerUpPickup | CheckpointTrigger |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Vehicle** | **YES** | **YES** | **YES** | **YES** | **YES (Trigger)** | **YES (Trigger)** |
| **Track_Ground** | **YES** | NO | NO | NO | NO | NO |
| **Track_Wall** | **YES** | NO | NO | **YES** | NO | NO |
| **Projectile** | **YES** | NO | **YES** | NO | NO | NO |
| **PowerUpPickup**| **YES** | NO | NO | NO | NO | NO |
| **Checkpoint** | **YES** | NO | NO | NO | NO | NO |

---

## 3. Raycast LayerMasks
* `GroundLayerMask` in `VehicleController`: **Layer 9 (`Track_Ground`)** only. Prevents raycasts from accidentally latching onto rival vehicle colliders or floating power-ups.
