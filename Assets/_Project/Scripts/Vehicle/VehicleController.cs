using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// Production vehicle controller using raycast suspension (our locked physics approach —
    /// see VEHICLE PHYSICS APPROACH decision, not WheelCollider).
    ///
    /// Phase 2 scope:
    ///   - Raycast suspension per wheel (ride height, spring force, damping)
    ///   - Forward acceleration + steering applied at wheel contact points
    ///   - Reads all tunable values from VehicleDataSO (no hard-coded stats)
    ///
    /// Phase 3 update:
    ///   - Input now comes from any IVehicleInputSource (KeyboardVehicleInput for
    ///     editor/desktop, TouchVehicleInput for mobile) rather than a hard-coded debug block.
    ///   - VehicleController has zero knowledge of *how* input is captured — decoupling
    ///     enables AI, touch, and gamepad to drive the exact same physics code.
    ///
    /// NOT in scope yet (later phases):
    ///   - Nitro, weapons, health — fields already reserved on VehicleDataSO.
    /// 
    /// Serves Pillars: "One-Thumb Mastery" & "Personality Over Realism".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private VehicleDataSO vehicleData;

        [Header("Wheel Sockets (assign 4: FL, FR, RL, RR)")]
        [SerializeField] private Transform[] wheelSockets = new Transform[4];
        [Tooltip("Which sockets steer. Default: first two (front wheels).")]
        [SerializeField] private int steeringWheelCount = 2;

        [Header("Input")]
        [Tooltip("Assign a KeyboardVehicleInput or TouchVehicleInput component (or any IVehicleInputSource). " +
                 "If left empty, this vehicle will not respond to input — useful for AI-driven vehicles later, " +
                 "which will supply their own AIController implementing this same interface.")]
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        private IVehicleInputSource inputSource;

        private Rigidbody rb;
        private float currentSteerAngle;
        private bool controlsLocked;
        private NitroSystem nitroSystem;

        // Per-wheel runtime state, exposed read-only for debugging/visuals (e.g. wheel spin, suspension compression)
        public bool[] WheelGrounded { get; private set; }
        public VehicleDataSO VehicleData => vehicleData;
        public IVehicleInputSource InputSource => inputSource;
        public bool ControlsLocked => controlsLocked;
        public NitroSystem NitroSystem => nitroSystem;

        /// <summary>
        /// Called by RaceCountdownController during Staging/CountingDown (Phase 6). While locked,
        /// throttle/steer are forced to zero and brake is force-held, so a car on a banked or sloped
        /// grid position doesn't drift before the race actually starts — this is a physics-level lock,
        /// not just "ignore input," because gravity/slope forces still act on the rigidbody regardless
        /// of what the input source reports.
        /// </summary>
        public void SetControlsLocked(bool locked)
        {
            controlsLocked = locked;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (vehicleData != null)
            {
                rb.mass = vehicleData.mass;
            }
            // Lower center of mass for arcade stability — standard trick to reduce tipping at speed.
            rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

            WheelGrounded = new bool[wheelSockets.Length];
            nitroSystem = GetComponent<NitroSystem>();

            if (inputSourceBehaviour != null)
            {
                inputSource = inputSourceBehaviour as IVehicleInputSource;
                if (inputSource == null)
                {
                    Debug.LogError($"{name}: assigned inputSourceBehaviour does not implement IVehicleInputSource.", this);
                }
            }
        }

        /// <summary>
        /// Allows runtime assignment of input source (used by spawners, test harnesses, and AI controllers).
        /// </summary>
        public void SetInputSource(IVehicleInputSource source)
        {
            inputSource = source;
            inputSourceBehaviour = source as MonoBehaviour;
        }

        private void FixedUpdate()
        {
            if (vehicleData == null) return;

            float throttleInput;
            float steerInput;
            bool brakeInput;
            bool nitroRequested;

            if (controlsLocked)
            {
                // Auto-brake hold: force brake regardless of what the input source says, so
                // slope/gravity can't roll the car during Staging/CountingDown. Nitro is locked
                // out too — no boosting before the race actually starts.
                throttleInput = 0f;
                steerInput = 0f;
                brakeInput = true;
                nitroRequested = false;
            }
            else
            {
                throttleInput = inputSource != null ? inputSource.Throttle : 0f;
                steerInput = inputSource != null ? inputSource.Steer : 0f;
                brakeInput = inputSource != null && inputSource.Brake;
                nitroRequested = inputSource != null && inputSource.NitroRequested;
            }

            bool isBoosting = nitroSystem != null && nitroSystem.Tick(nitroRequested, Time.fixedDeltaTime);

            ApplySuspensionAndDrive(throttleInput, steerInput, brakeInput, isBoosting);
        }

        private void ApplySuspensionAndDrive(float throttleInput, float steerInput, bool brakeInput, bool isBoosting)
        {
            Vector3 currentVelocity = GetLinearVelocity();

            float boostMultiplier = isBoosting ? vehicleData.nitroBoostMultiplier : 1f;
            float effectiveTopSpeed = vehicleData.topSpeed * boostMultiplier;
            float effectiveAccelerationForce = vehicleData.accelerationForce * boostMultiplier;

            // Smoothly approach target steer angle, reduced at high speed for stability.
            float speedFactor = Mathf.Clamp01(currentVelocity.magnitude / effectiveTopSpeed);
            float steerRetention = Mathf.Lerp(1f, vehicleData.highSpeedSteerRetention, speedFactor);
            float targetSteerAngle = steerInput * vehicleData.maxSteerAngle * steerRetention;
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * vehicleData.steerSpeed);

            int groundedCount = 0;

            for (int i = 0; i < wheelSockets.Length; i++)
            {
                Transform wheel = wheelSockets[i];
                if (wheel == null) continue;

                // Rotate steering wheels visually + for lateral force direction.
                bool isSteeringWheel = i < steeringWheelCount;
                if (isSteeringWheel)
                {
                    wheel.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
                }

                float maxRayLength = vehicleData.suspensionRestDistance + vehicleData.wheelRadius;

                if (Physics.Raycast(wheel.position, -transform.up, out RaycastHit hit, maxRayLength))
                {
                    WheelGrounded[i] = true;
                    groundedCount++;

                    // --- Suspension spring + damper force ---
                    float compression = 1f - (hit.distance - vehicleData.wheelRadius) / vehicleData.suspensionRestDistance;
                    compression = Mathf.Clamp01(compression);

                    Vector3 wheelWorldVel = rb.GetPointVelocity(wheel.position);
                    float springVelocity = Vector3.Dot(transform.up, wheelWorldVel);

                    float springForce = compression * vehicleData.springStrength;
                    float damperForce = -springVelocity * vehicleData.springDamper;

                    Vector3 suspensionForce = transform.up * (springForce + damperForce);
                    rb.AddForceAtPosition(suspensionForce, wheel.position);

                    // --- Drive force (forward/back along the vehicle's forward direction, applied at wheel) ---
                    if (Mathf.Abs(throttleInput) > 0.01f && !brakeInput)
                    {
                        float currentForwardSpeed = Vector3.Dot(currentVelocity, transform.forward);
                        if (Mathf.Abs(currentForwardSpeed) < effectiveTopSpeed)
                        {
                            Vector3 driveForce = transform.forward * (throttleInput * effectiveAccelerationForce);
                            rb.AddForceAtPosition(driveForce, wheel.position);
                        }
                    }
                    else if (brakeInput)
                    {
                        Vector3 forwardVel = Vector3.Project(currentVelocity, transform.forward);
                        rb.AddForceAtPosition(-forwardVel.normalized * vehicleData.brakeForce, wheel.position);
                    }
                    else
                    {
                        // Coasting drag — passive deceleration so the car doesn't roll forever.
                        Vector3 forwardVel = Vector3.Project(currentVelocity, transform.forward);
                        rb.AddForceAtPosition(-forwardVel * vehicleData.coastingDrag * 0.1f, wheel.position);
                    }

                    // --- Grip: cancel a portion of sideways (lateral) velocity at this wheel ---
                    Vector3 lateralVel = Vector3.Project(wheelWorldVel, transform.right);
                    Vector3 gripCancelForce = -lateralVel * vehicleData.gripFactor * rb.mass * 0.5f;
                    rb.AddForceAtPosition(gripCancelForce, wheel.position);
                }
                else
                {
                    WheelGrounded[i] = false;
                }
            }
        }

        private Vector3 GetLinearVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        // Small editor helper so we can see suspension raycasts while tuning.
        private void OnDrawGizmosSelected()
        {
            if (wheelSockets == null || vehicleData == null) return;
            Gizmos.color = Color.yellow;
            foreach (var wheel in wheelSockets)
            {
                if (wheel == null) continue;
                float maxRayLength = vehicleData.suspensionRestDistance + vehicleData.wheelRadius;
                Gizmos.DrawLine(wheel.position, wheel.position - transform.up * maxRayLength);
            }
        }
    }
}
