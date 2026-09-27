# Animal Combat Racing — Combat System Specification

## 1. Overview
Fictional, family-friendly cartoon vehicle combat designed for tactical arcade excitement.

## 2. Core Weapon Categories
1. **Straight Projectile** (`Kinetic Energy Orb`): Direct skill-shot; spins target on impact.
2. **Homing Projectile** (`Homing Stinger`): Locks on and chases nearest opponent ahead.
3. **Hazard Drop** (`Rolling Shock Mine`): Dropped behind or bowled forward.
4. **Defensive Ability** (`Aura Shield`): Absorbs 1 incoming weapon hit.
5. **Character Special Ability** (`Apex Sonic Roar`): Radial shockwave that flips nearby karts.

## 3. Interfaces
* `IWeapon`: Interface implemented by all weapon prefabs.
* `IDamageable`: Interface implemented by all vehicles receiving combat hits.
