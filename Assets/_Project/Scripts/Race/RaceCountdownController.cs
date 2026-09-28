using System;
using System.Collections;
using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Race
{
    public enum CountdownState
    {
        Staging,
        CountingDown,
        Racing
    }

    /// <summary>
    /// Drives the race start sequence: places vehicles on the grid, locks controls, counts down
    /// 3-2-1, then releases every vehicle's controls in the same frame so nobody gets a head start.
    /// Phase 6 scope only — does not know about laps, positions, or race completion (Phase 7),
    /// and does not know about AI decision-making (Phase 8), only that AI vehicles' VehicleController
    /// gets locked/unlocked exactly like the player's.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public class RaceCountdownController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrackManager trackManager;
        [Tooltip("Index in this array must match the grid slot each vehicle starts in.")]
        [SerializeField] private VehicleController[] racers;
        [SerializeField] private ThirdPersonCameraController playerCamera;
        [SerializeField] private int playerGridSlot = 0;

        [Header("Timing")]
        [SerializeField] private float countdownTickInterval = 1f;

        public CountdownState State { get; private set; } = CountdownState.Staging;
        public float RaceTime { get; private set; }
        public VehicleController[] Racers => racers;
        public TrackManager Track => trackManager;
        public int PlayerGridSlot => playerGridSlot;

        /// <summary>Fired once per tick with the number being displayed (3, 2, 1).</summary>
        public static event Action<int> OnCountdownTick;
        /// <summary>Fired the instant controls unlock — UI/audio ("GO!") and the race timer both key off this.</summary>
        public static event Action OnRaceStarted;
        /// <summary>Fired once grid placement + control-locking is done, before counting begins.</summary>
        public static event Action OnStagingComplete;

        private bool raceTimerRunning;

        public void Configure(TrackManager tm, VehicleController[] raceVehicles, ThirdPersonCameraController cam, int slot = 0)
        {
            trackManager = tm;
            racers = raceVehicles;
            playerCamera = cam;
            playerGridSlot = slot;
        }

        private void Start()
        {
            BeginSequence();
        }

        /// <summary>Re-runs the full staging + countdown sequence. Safe to call again for a restart.</summary>
        public void BeginSequence()
        {
            StopAllCoroutines();
            raceTimerRunning = false;
            RaceTime = 0f;

            StageVehicles();
            StartCoroutine(CountdownRoutine());
        }

        private void StageVehicles()
        {
            State = CountdownState.Staging;

            if (trackManager == null)
            {
                Debug.LogError($"{name}: no TrackManager assigned — cannot stage vehicles.", this);
                return;
            }

            if (racers == null || racers.Length == 0)
            {
                Debug.LogWarning($"{name}: no racers assigned to grid.", this);
                return;
            }

            for (int i = 0; i < racers.Length; i++)
            {
                VehicleController vehicle = racers[i];
                if (vehicle == null) continue;

                Vector3 startPos = trackManager.GetStartPosition(i);
                Quaternion startRot = trackManager.GetStartRotation(i);
                vehicle.transform.SetPositionAndRotation(startPos, startRot);

                Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    ResetRigidbodyVelocity(rb);
                }

                vehicle.SetControlsLocked(true);

                if (i == playerGridSlot && playerCamera != null)
                {
                    playerCamera.SetTarget(vehicle.transform);
                    playerCamera.SnapToTarget();
                }
            }

            OnStagingComplete?.Invoke();
        }

        private IEnumerator CountdownRoutine()
        {
            State = CountdownState.CountingDown;

            for (int count = 3; count >= 1; count--)
            {
                OnCountdownTick?.Invoke(count);
                yield return new WaitForSeconds(countdownTickInterval);
            }

            StartRace();
        }

        private void StartRace()
        {
            State = CountdownState.Racing;

            // Unlock every vehicle in the same pass, same frame — no sequential unlocking that
            // could give grid slot 0 even a one-frame advantage over slot 3.
            if (racers != null)
            {
                foreach (VehicleController vehicle in racers)
                {
                    if (vehicle != null)
                    {
                        vehicle.SetControlsLocked(false);
                    }
                }
            }

            raceTimerRunning = true;
            OnRaceStarted?.Invoke();
        }

        private void Update()
        {
            if (raceTimerRunning)
            {
                RaceTime += Time.deltaTime;
            }
        }

        private void ResetRigidbodyVelocity(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
    }
}
