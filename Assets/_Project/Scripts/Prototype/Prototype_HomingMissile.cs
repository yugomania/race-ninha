using UnityEngine;

namespace ACR.Prototype
{
    /// <summary>
    /// PHASE 0.5 THROWAWAY PROTOTYPE HOMING WEAPON
    /// Purpose: Test if firing a simple homing projectile at dummy targets feels punchy and readable.
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public class Prototype_HomingMissile : MonoBehaviour
    {
        public float speed = 40f;
        public float turnRate = 120f;
        public float lifeTime = 3.5f;
        public float blastRadius = 5f;
        public float blastImpulse = 20f;

        private Transform _target;
        private Rigidbody _rb;

        public void Initialize(Transform target)
        {
            _target = target;
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            Destroy(gameObject, lifeTime);
        }

        private void FixedUpdate()
        {
            if (_target != null)
            {
                Vector3 toTarget = (_target.position - transform.position).normalized;
                Vector3 newDir = Vector3.RotateTowards(transform.forward, toTarget, turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
                transform.rotation = Quaternion.LookRotation(newDir);
            }

            if (_rb != null)
            {
                _rb.linearVelocity = transform.forward * speed;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Explode();
        }

        private void Explode()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
            foreach (var col in colliders)
            {
                var vehicle = col.GetComponent<Prototype_ArcadeVehicle>();
                if (vehicle != null)
                {
                    vehicle.ApplyImpactSpinout();
                }

                var rb = col.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddExplosionForce(blastImpulse, transform.position, blastRadius, 1.2f, ForceMode.Impulse);
                }
            }

            Destroy(gameObject);
        }
    }
}
