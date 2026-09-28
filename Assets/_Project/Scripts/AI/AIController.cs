using UnityEngine;
using ACR.Race;
using ACR.Vehicle;

namespace ACR.AI
{
    /// <summary>
    /// Simplest possible AI driver, per Phase 8 scope: follow racing path (reusing existing
    /// checkpoints) → detect checkpoints → finish race. Implements IVehicleInputSource exactly
    /// like KeyboardVehicleInput/TouchVehicleInput, which is why VehicleController needed zero
    /// changes to support AI opponents — it never knew or cared what was providing input.
    ///
    /// NOT in scope yet (later, per roadmap): overtaking, collision avoidance, nitro/weapon use,
    /// recovery after crashes, braking before corners. This version can and likely will spin out
    /// or clip walls on sharper corners — that's expected at this stage, not a bug to fix now.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class AIController : MonoBehaviour, IVehicleInputSource
    {
        [SerializeField] private TrackManager trackManager;

        [Header("Steering")]
        [Tooltip("How sharply the AI reacts to its angle to the target checkpoint. Higher = twitchier.")]
        [SerializeField] private float steerSensitivity = 1.5f;

        [Header("Throttle")]
        [Range(0f, 1f)] [SerializeField] private float baseThrottle = 1f;
        [Tooltip("0-1. How much throttle is cut during sharp turns, so this simplest-possible AI " +
                 "doesn't take every corner at full speed. Pure MVP heuristic, not real cornering AI.")]
        [Range(0f, 1f)] [SerializeField] private float cornerSlowdownFactor = 0.4f;

        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Brake { get; private set; }

        public int TargetCheckpointIndex => targetCheckpointIndex;
        public TrackManager Track => trackManager;

        private VehicleController selfVehicle;
        private int targetCheckpointIndex;

        public void Configure(TrackManager tm)
        {
            trackManager = tm;
            ResetTarget();
        }

        private void Awake()
        {
            selfVehicle = GetComponent<VehicleController>();
        }

        private void OnEnable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint += HandleCheckpointPassed;
            RaceCountdownController.OnStagingComplete += ResetTarget;
        }

        private void OnDisable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint -= HandleCheckpointPassed;
            RaceCountdownController.OnStagingComplete -= ResetTarget;
        }

        private void ResetTarget()
        {
            if (trackManager == null) return;
            // Same starting convention as LapSystem: aim past the finish line first, not at it,
            // since the grid starts just behind checkpoint 0.
            targetCheckpointIndex = trackManager.CheckpointCount > 1 ? 1 : 0;
        }

        private void HandleCheckpointPassed(TrackCheckpoint checkpoint, VehicleController vehicle)
        {
            if (selfVehicle == null || vehicle != selfVehicle) return;
            if (checkpoint.checkpointIndex != targetCheckpointIndex) return;

            targetCheckpointIndex = (checkpoint.checkpointIndex + 1) % trackManager.CheckpointCount;
        }

        private void Update()
        {
            if (trackManager == null || trackManager.CheckpointCount == 0)
            {
                Throttle = 0f;
                Steer = 0f;
                Brake = false;
                return;
            }

            Transform target = trackManager.Checkpoints[targetCheckpointIndex].transform;
            Vector3 toTarget = target.position - transform.position;
            Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
            float angleToTarget = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

            Steer = Mathf.Clamp((angleToTarget / 45f) * steerSensitivity, -1f, 1f);

            float steerMagnitude = Mathf.Abs(Steer);
            Throttle = Mathf.Lerp(baseThrottle, baseThrottle * (1f - cornerSlowdownFactor), steerMagnitude);

            Brake = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (trackManager == null || trackManager.CheckpointCount == 0) return;
            TrackCheckpoint[] cps = trackManager.Checkpoints;
            if (cps != null && targetCheckpointIndex >= 0 && targetCheckpointIndex < cps.Length && cps[targetCheckpointIndex] != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, cps[targetCheckpointIndex].transform.position);
            }
        }
    }
}
