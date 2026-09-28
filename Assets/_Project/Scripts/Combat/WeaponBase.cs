using UnityEngine;
using ACR.Vehicle;

namespace ACR.Combat
{
    /// <summary>
    /// Base class for all weapons. Handles cooldown, input reading, and respecting the vehicle's
    /// controls-lock — every future weapon (Phase 18) only needs to implement Fire(). This is the
    /// modular weapon framework the system prompt calls for: adding a new weapon later should never
    /// require touching this class or any existing weapon.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected float cooldownDuration = 2f;

        protected float cooldownRemaining;
        protected VehicleController vehicleController;
        protected IVehicleInputSource inputSource;

        public bool IsReady => cooldownRemaining <= 0f;
        public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);
        public float CooldownDuration => cooldownDuration;

        protected virtual void Start()
        {
            vehicleController = GetComponent<VehicleController>();
            // Fetched from VehicleController rather than wired separately in the inspector — see
            // the InputSource property's doc comment on VehicleController for why this is safe here.
            if (vehicleController != null) inputSource = vehicleController.InputSource;
        }

        protected virtual void Update()
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }

            bool wantsFire = inputSource != null && inputSource.WeaponFireRequested
                              && vehicleController != null && !vehicleController.ControlsLocked;

            if (wantsFire && IsReady)
            {
                Fire(vehicleController);
                cooldownRemaining = cooldownDuration;
            }
        }

        /// <summary>
        /// Implement the actual firing behavior. Called only when cooldown is ready and
        /// controls aren't locked — subclasses don't need to re-check either.
        /// </summary>
        protected abstract void Fire(VehicleController firer);
    }
}
