using UnityEngine;

namespace ACR.Core
{
    /// <summary>
    /// Third-person chase camera. Phase 4 scope:
    ///   - Smoothed follow/rotation behind a target vehicle using SmoothDamp
    ///   - Slight look-ahead based on vehicle turning delta, so camera "leans into" turns
    ///   - Public shake + FOV-kick hooks, built now so Phase 9 (nitro) and Phase 10
    ///     (weapons/impacts) can call TriggerShake / TriggerFOVKick directly without
    ///     reinventing screen-space feedback.
    ///
    /// Temporary debug trigger (C key) included so we can visually confirm shake/FOV
    /// kick work before Phase 9/10 exist to call them for real.
    ///
    /// Serves Pillars: "Readable Chaos" & "One-Thumb Mastery".
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCameraController : MonoBehaviour
    {
        public static ThirdPersonCameraController Instance { get; private set; }

        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow")]
        [SerializeField] private float followDistance = 8f;
        [SerializeField] private float followHeight = 3.5f;
        [Tooltip("Smaller = snappier follow, larger = floatier.")]
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float rotationSmoothSpeed = 6f;

        [Header("Look-Ahead")]
        [Tooltip("How much the camera shifts sideways/rotates based on target's turning rate.")]
        [SerializeField] private float lookAheadAmount = 1.5f;
        [SerializeField] private float lookAheadSmoothSpeed = 4f;

        [Header("FOV")]
        [SerializeField] private float baseFOV = 60f;

        [Header("Debug (temporary — remove once Phase 9/10 wire real triggers)")]
        [SerializeField] private bool enableDebugTrigger = true;

        private Camera cam;
        private Vector3 followVelocity; // used by SmoothDamp
        private float currentLookAhead;
        private Vector3 lastTargetForward;

        // --- Shake state ---
        private float shakeTimeRemaining;
        private float shakeDuration;
        private float shakeIntensity;
        private Vector3 shakeOffset;

        // --- FOV kick state ---
        private float fovKickTimeRemaining;
        private float fovKickDuration;
        private float fovKickAmount;

        // Public getters for telemetry / UI / test harnesses
        public Transform Target => target;
        public float CurrentFOV => cam != null ? cam.fieldOfView : baseFOV;
        public bool IsShaking => shakeTimeRemaining > 0f;
        public bool IsFOVKicking => fovKickTimeRemaining > 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = baseFOV;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (target != null)
            {
                lastTargetForward = target.forward;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateFollowPosition();
            UpdateFollowRotation();
            UpdateShake();
            UpdateFOVKick();

            if (enableDebugTrigger && Input.GetKeyDown(KeyCode.C))
            {
                TriggerShake(0.3f, 0.25f);
                TriggerFOVKick(10f, 0.3f);
            }
        }

        private void UpdateFollowPosition()
        {
            Vector3 desiredPosition = target.position
                                       - target.forward * followDistance
                                       + Vector3.up * followHeight;

            // Remove shake offset prior to SmoothDamp to prevent jitter accumulation in followVelocity
            Vector3 unperturbedPos = transform.position - shakeOffset;
            Vector3 smoothedPos = Vector3.SmoothDamp(unperturbedPos, desiredPosition, ref followVelocity, positionSmoothTime);

            // Apply shake as a local offset on top of smoothed position
            transform.position = smoothedPos + shakeOffset;
        }

        private void UpdateFollowRotation()
        {
            if (lastTargetForward == Vector3.zero)
            {
                lastTargetForward = target.forward;
            }

            // Approximate turning rate (deg/sec) from target heading change between frames
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float yawDelta = Vector3.SignedAngle(lastTargetForward, target.forward, Vector3.up);
            float targetLean = Mathf.Clamp(yawDelta / dt, -120f, 120f);
            lastTargetForward = target.forward;

            currentLookAhead = Mathf.Lerp(currentLookAhead, targetLean, Time.deltaTime * lookAheadSmoothSpeed);

            Vector3 lookAtPoint = target.position + target.right * (currentLookAhead * 0.01f * lookAheadAmount) + Vector3.up * 1f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSmoothSpeed);
        }

        private void UpdateShake()
        {
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                float falloff = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
                shakeOffset = Random.insideUnitSphere * shakeIntensity * falloff;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        private void UpdateFOVKick()
        {
            if (cam == null) return;

            if (fovKickTimeRemaining > 0f)
            {
                fovKickTimeRemaining -= Time.deltaTime;
                float t = Mathf.Clamp01(fovKickTimeRemaining / fovKickDuration);
                cam.fieldOfView = baseFOV + fovKickAmount * t;
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFOV, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// Called by nitro, weapon impacts, collisions, etc. Screen-space impact feedback.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeDuration = Mathf.Max(duration, 0.01f);
            shakeTimeRemaining = shakeDuration;
        }

        /// <summary>
        /// Called by nitro activation, boost pads, warp gates, etc.
        /// </summary>
        public void TriggerFOVKick(float amount, float duration)
        {
            fovKickAmount = amount;
            fovKickDuration = Mathf.Max(duration, 0.01f);
            fovKickTimeRemaining = fovKickDuration;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetForward = target.forward;
                currentLookAhead = 0f;
            }
        }

        /// <summary>
        /// Instantly snaps the camera behind the target without smoothing lag.
        /// Useful for race start, respawns, or teleportation.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 desiredPosition = target.position - target.forward * followDistance + Vector3.up * followHeight;
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            lastTargetForward = target.forward;
            currentLookAhead = 0f;
            shakeOffset = Vector3.zero;
            shakeTimeRemaining = 0f;

            Vector3 lookAtPoint = target.position + Vector3.up * 1f;
            transform.rotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
        }
    }
}
