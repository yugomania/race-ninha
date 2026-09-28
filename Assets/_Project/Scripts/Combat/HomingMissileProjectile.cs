using System;
using UnityEngine;
using ACR.Vehicle;

namespace ACR.Combat
{
    /// <summary>
    /// Pooled homing projectile. Flies forward and steers toward its target at a limited turn
    /// rate (arcade homing, not an instant lock-on) — this is an arcade-game weapon per the system
    /// prompt, not a realistic missile simulation. On hit or timeout, returns itself to the pool
    /// via a callback rather than being destroyed, so the pool never needs to Instantiate/Destroy
    /// during actual gameplay.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HomingMissileProjectile : MonoBehaviour
    {
        private Transform target;
        private VehicleController firer;
        private float speed;
        private float turnRateDegreesPerSec;
        private float lifetimeRemaining;
        private Action<HomingMissileProjectile> returnToPool;

        public Transform Target => target;
        public VehicleController Firer => firer;

        /// <summary>Called by HomingMissileWeapon each time this instance is taken from the pool.</summary>
        public void Launch(Vector3 position, Quaternion rotation, Transform target, VehicleController firer,
                            float speed, float turnRateDegreesPerSec, float lifetime,
                            Action<HomingMissileProjectile> returnToPool)
        {
            transform.SetPositionAndRotation(position, rotation);
            this.target = target;
            this.firer = firer;
            this.speed = speed;
            this.turnRateDegreesPerSec = turnRateDegreesPerSec;
            this.lifetimeRemaining = lifetime;
            this.returnToPool = returnToPool;
        }

        private void Update()
        {
            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Expire();
                return;
            }

            if (target != null)
            {
                Vector3 desiredDirection = (target.position - transform.position).normalized;
                Vector3 newDirection = Vector3.RotateTowards(
                    transform.forward, desiredDirection,
                    turnRateDegreesPerSec * Mathf.Deg2Rad * Time.deltaTime, 0f);
                transform.rotation = Quaternion.LookRotation(newDirection);
            }

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            VehicleController hitVehicle = other.GetComponentInParent<VehicleController>();
            if (hitVehicle == null || hitVehicle == firer) return;

            // Phase 12 (health/damage system) will apply real damage here and trigger camera-hit
            // feedback. For now this is a clearly marked stub: the hit is detected and the missile
            // despawns, but nothing is actually damaged yet since VehicleHealth doesn't exist.
            Debug.Log($"[Phase10 stub] {hitVehicle.name} was hit by a homing missile from {firer?.name} " +
                      "— real damage lands in Phase 12.");

            Expire();
        }

        private void Expire()
        {
            target = null;
            firer = null;
            var callback = returnToPool;
            returnToPool = null;
            callback?.Invoke(this);
        }
    }
}
