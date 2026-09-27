using UnityEngine;

namespace ACR.Prototype
{
    /// <summary>
    /// PHASE 0.5 THROWAWAY PROTOTYPE VEHICLE
    /// Purpose: Test if raycast arcade driving + drifting feels fun in 5 minutes.
    /// Note: This script is disposable and isolated from the final production architecture.
    /// Serves Pillars: "One-Thumb Mastery" & "Personality Over Realism".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Prototype_ArcadeVehicle : MonoBehaviour
    {
        [Header("Engine & Speed")]
        public float topSpeed = 30f; // ~108 km/h
        public float acceleration = 25f;
        public float reverseSpeed = 10f;
        public float brakeForce = 35f;

        [Header("Arcade Steering & Drift")]
        public float steerSpeed = 90f;
        [Range(0f, 1f)] public float normalGrip = 0.95f;
        [Range(0f, 1f)] public float driftGrip = 0.80f;
        public float driftBoostImpulse = 10f;

        [Header("Raycast Suspension")]
        public float suspensionRestDist = 0.6f;
        public float springStrength = 150f;
        public float springDamper = 12f;
        public LayerMask groundLayer;

        [Header("Jump & Air Control")]
        public float extraGravity = 15f;

        [Header("Input (Keyboard or Virtual)")]
        public float steerInput;
        public float throttleInput;
        public bool isDrifting;
        public bool isBraking;

        private Rigidbody _rb;
        private bool _isGrounded;
        private float _driftTimer;

        // 4 raycast socket offsets (Front-Left, Front-Right, Rear-Left, Rear-Right)
        private readonly Vector3[] _wheelOffsets = new Vector3[]
        {
            new Vector3(-0.9f, 0f, 1.2f),
            new Vector3(0.9f, 0f, 1.2f),
            new Vector3(-0.95f, 0f, -1.2f),
            new Vector3(0.95f, 0f, -1.2f)
        };

        public float CurrentSpeedKmh => _rb != null ? _rb.linearVelocity.magnitude * 3.6f : 0f;
        public bool IsGrounded => _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 650f;
            _rb.centerOfMass = new Vector3(0f, -0.4f, 0f); // Low center of gravity prevents rolling
        }

        private void Update()
        {
            // Simple default input mapping for rapid testing
            steerInput = Input.GetAxis("Horizontal");
            throttleInput = Input.GetAxis("Vertical");
            isDrifting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }

        private void FixedUpdate()
        {
            ApplySuspensionForces();

            if (_isGrounded)
            {
                ApplyDriveForces();
                ApplySteeringForces();
                ApplyLateralFriction();
            }
            else
            {
                ApplyAirStabilization();
            }
        }

        private void ApplySuspensionForces()
        {
            int groundedWheels = 0;

            for (int i = 0; i < _wheelOffsets.Length; i++)
            {
                Vector3 worldRayOrigin = transform.TransformPoint(_wheelOffsets[i]);
                RaycastHit hit;

                if (Physics.Raycast(worldRayOrigin, -transform.up, out hit, suspensionRestDist + 0.3f, groundLayer))
                {
                    groundedWheels++;
                    float compression = suspensionRestDist - hit.distance;
                    Vector3 springDir = transform.up;
                    Vector3 pointVelocity = _rb.GetPointVelocity(worldRayOrigin);
                    float damperForce = Vector3.Dot(pointVelocity, springDir) * springDamper;
                    float finalSpringForce = (compression * springStrength) - damperForce;

                    _rb.AddForceAtPosition(springDir * Mathf.Max(0f, finalSpringForce), worldRayOrigin, ForceMode.Acceleration);
                }
            }

            _isGrounded = groundedWheels >= 2;
        }

        private void ApplyDriveForces()
        {
            float targetSpeed = topSpeed;
            float currentSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);

            if (throttleInput > 0.01f && currentSpeed < targetSpeed)
            {
                Vector3 forwardForce = transform.forward * (throttleInput * acceleration);
                _rb.AddForce(forwardForce, ForceMode.Acceleration);
            }
            else if (throttleInput < -0.01f && currentSpeed > -reverseSpeed)
            {
                Vector3 reverseForce = transform.forward * (throttleInput * brakeForce);
                _rb.AddForce(reverseForce, ForceMode.Acceleration);
            }
        }

        private void ApplySteeringForces()
        {
            if (Mathf.Abs(steerInput) < 0.01f) return;

            // Turn responsiveness scales with forward speed
            float speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / (topSpeed * 0.3f));
            float turnAngle = steerInput * steerSpeed * speedFactor * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAngle, 0f);
            _rb.MoveRotation(_rb.rotation * turnRotation);
        }

        private void ApplyLateralFriction()
        {
            float grip = isDrifting ? driftGrip : normalGrip;
            Vector3 sideVelocity = transform.right * Vector3.Dot(_rb.linearVelocity, transform.right);
            _rb.AddForce(-sideVelocity * grip, ForceMode.VelocityChange);

            // Drift boost reward
            if (isDrifting && Mathf.Abs(steerInput) > 0.2f)
            {
                _driftTimer += Time.fixedDeltaTime;
            }
            else
            {
                if (_driftTimer > 1.0f)
                {
                    // Mini turbo boost upon exiting drift
                    _rb.AddForce(transform.forward * driftBoostImpulse, ForceMode.VelocityChange);
                }
                _driftTimer = 0f;
            }
        }

        private void ApplyAirStabilization()
        {
            // Extra cartoon gravity for snappy jump landings
            _rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

            // Auto-align chassis horizontally in mid-air
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 3f);
        }

        public void ApplyImpactSpinout()
        {
            _rb.AddTorque(Vector3.up * 85f, ForceMode.Impulse);
            _rb.linearVelocity *= 0.45f; // Slow down without dead stop
        }
    }
}
