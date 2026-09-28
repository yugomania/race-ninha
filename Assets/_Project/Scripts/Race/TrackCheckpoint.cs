using System;
using UnityEngine;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// Trigger volume detecting vehicles crossing track checkpoints.
    /// Phase 5 scope:
    ///   - Trigger detection for VehicleController
    ///   - Fires static OnVehiclePassedCheckpoint event
    ///   - Pure fact reporting: zero knowledge of laps, race rules, or anti-cheat.
    ///     Phase 7 (LapSystem) will subscribe to this event.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrackCheckpoint : MonoBehaviour
    {
        public static event Action<TrackCheckpoint, VehicleController> OnVehiclePassedCheckpoint;

        [Header("Checkpoint Config")]
        [Tooltip("Contiguous 0-indexed sequence along the track.")]
        public int checkpointIndex = 0;

        [Tooltip("Exactly one checkpoint per track must be the finish line (index 0).")]
        public bool isFinishLine = false;

        private Collider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            VehicleController vehicle = null;
            if (other.attachedRigidbody != null)
            {
                vehicle = other.attachedRigidbody.GetComponent<VehicleController>();
            }
            if (vehicle == null)
            {
                vehicle = other.GetComponentInParent<VehicleController>();
            }

            if (vehicle != null)
            {
                OnVehiclePassedCheckpoint?.Invoke(this, vehicle);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isFinishLine ? new Color(0f, 1f, 0.3f, 0.35f) : new Color(0f, 0.7f, 1f, 0.25f);

            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = isFinishLine ? Color.green : Color.cyan;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 3f);
            }

            // Direction arrow indicating expected vehicle flow
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Gizmos.DrawRay(center, transform.forward * 4f);
        }
    }
}
