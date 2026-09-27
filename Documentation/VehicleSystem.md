# Animal Combat Racing — Vehicle System & Physics Decision

**Status:** LOCKED (Phase 1 Sign-Off)  
**Physics Model:** Simplified Arcade Movement (4-Point Raycast Suspension + Custom Rigidbody Forces)  
**WheelCollider Usage:** NONE (Strictly Prohibited)  

---

## 1. Locked Decision: Simplified Arcade Movement

We have evaluated both approaches against the project's 4 Design Pillars and mobile hardware constraints. The decision is formally locked:

### Why Simplified Arcade Raycast Suspension Was Chosen:
1. **Pillar 3 (One-Thumb Mastery)**:
   * Raycast suspension provides 100% deterministic, controllable handling curves.
   * Enables immediate, responsive drift steering with zero simulation latency.
   * `WheelCollider` models complex tire slip curves ($Pacejka$) designed for simulation racing, which feel unresponsive, sluggish, or erratic with mobile touch inputs.

2. **Pillar 4 (Personality Over Realism)**:
   * Exaggerated cartoon jump dynamics, springy suspension squash/stretch, and lateral bump impulses are trivial to compute and tune with custom forces.
   * Prevents realistic vehicle rollovers. `WheelCollider` vehicles frequently flip upside down when hitting curbs at high speeds.

3. **Mobile Performance (Samsung Galaxy A53 5G Reference)**:
   * 4 downward raycasts per kart have virtually zero CPU footprint.
   * PhysX `WheelCollider` runs sub-stepping calculations on internal friction models, causing noticeable frame drops when 8 karts cluster together on mobile chips.

---

## 2. Mathematical Suspension Model

Each vehicle executes 4 downward raycasts from predefined wheel anchor sockets ($FL, FR, RL, RR$):

$$F_{\text{suspension}} = \max\left(0, (L_{\text{rest}} - d_{\text{hit}}) \cdot k_{\text{spring}} - v_{\text{relative}} \cdot c_{\text{damper}}\right)$$

Where:
* $L_{\text{rest}}$ = Resting suspension spring length (typically 0.55m).
* $d_{\text{hit}}$ = Raycast hit distance to track geometry.
* $k_{\text{spring}}$ = Spring stiffness coefficient ($120 - 180\text{ N/m}$).
* $c_{\text{damper}}$ = Damping coefficient ($8 - 14\text{ Ns/m}$) to eliminate bouncing oscillations.

---

## 3. Lateral Friction & Drift Model

Lateral tire grip is modeled by dynamically canceling perpendicular velocity:

$$\vec{v}_{\text{lateral}} = (\vec{v} \cdot \hat{r}) \hat{r}$$
$$\vec{F}_{\text{grip}} = -\vec{v}_{\text{lateral}} \cdot \mu_{\text{grip}} \cdot M$$

Where:
* $\hat{r}$ = Car's local right vector.
* $\mu_{\text{grip}}$ = Dynamic grip factor ($0.95$ in normal driving; $0.75 - 0.82$ during active drift).
* $M$ = Vehicle mass.

When exiting a drift of $\ge 1.0\text{ second}$, a mini-turbo forward impulse is applied:
$$\vec{F}_{\text{boost}} = \hat{f} \cdot I_{\text{driftBoost}}$$
