using System;
using System.Collections.Generic;
using UnityEngine;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// Per-vehicle lap-tracking state. Plain data, no logic — LapSystem owns all the rules.
    /// </summary>
    public class LapProgress
    {
        public int nextExpectedCheckpointIndex;
        public int lapsCompleted;
        public bool finished;
    }

    /// <summary>
    /// Validates that checkpoints are crossed in the correct order (closing the direction/order gap
    /// left open in Phase 5) and counts completed laps. Phase 7 scope: laps and finish detection only —
    /// RaceManager decides what "finish" means for win/lose, LapSystem just reports facts.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public class LapSystem : MonoBehaviour
    {
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private RaceCountdownController countdownController;

        private readonly Dictionary<VehicleController, LapProgress> progress = new Dictionary<VehicleController, LapProgress>();

        /// <summary>Fired when a vehicle crosses the finish line to complete a lap (not necessarily the final one).</summary>
        public static event Action<VehicleController, int> OnLapCompleted;
        /// <summary>Fired once, when a vehicle completes its final required lap. Carries the race-clock finish time.</summary>
        public static event Action<VehicleController, float> OnVehicleFinished;

        public void Configure(TrackManager tm, RaceCountdownController cc)
        {
            trackManager = tm;
            countdownController = cc;
        }

        private void OnEnable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint += HandleCheckpointPassed;
            RaceCountdownController.OnStagingComplete += ResetProgress;
        }

        private void OnDisable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint -= HandleCheckpointPassed;
            RaceCountdownController.OnStagingComplete -= ResetProgress;
        }

        public void ResetProgress()
        {
            progress.Clear();
            if (countdownController == null || trackManager == null) return;

            int totalCheckpoints = trackManager.CheckpointCount;
            if (countdownController.Racers == null) return;

            foreach (VehicleController vehicle in countdownController.Racers)
            {
                if (vehicle == null) continue;
                progress[vehicle] = new LapProgress
                {
                    // Grid starts just behind the finish line (checkpoint 0), so the first checkpoint
                    // actually ahead of the racers is index 1 — expecting index 0 immediately would
                    // register a false lap completion at the start of the race.
                    nextExpectedCheckpointIndex = totalCheckpoints > 1 ? 1 : 0,
                    lapsCompleted = 0,
                    finished = false
                };
            }
        }

        private void HandleCheckpointPassed(TrackCheckpoint checkpoint, VehicleController vehicle)
        {
            if (countdownController == null || countdownController.State != CountdownState.Racing) return;
            if (vehicle == null || !progress.TryGetValue(vehicle, out LapProgress p) || p.finished) return;

            // Wrong checkpoint (out of order, or driving backward through the right one but from
            // the wrong side) — ignore silently. This is the order/direction validation Phase 5 deferred.
            if (checkpoint.checkpointIndex != p.nextExpectedCheckpointIndex) return;

            int totalCheckpoints = trackManager.CheckpointCount;
            p.nextExpectedCheckpointIndex = (checkpoint.checkpointIndex + 1) % totalCheckpoints;

            if (checkpoint.isFinishLine)
            {
                p.lapsCompleted++;
                OnLapCompleted?.Invoke(vehicle, p.lapsCompleted);

                if (p.lapsCompleted >= trackManager.LapCount)
                {
                    p.finished = true;
                    OnVehicleFinished?.Invoke(vehicle, countdownController.RaceTime);
                }
            }
        }

        public bool TryGetProgress(VehicleController vehicle, out LapProgress result)
        {
            return progress.TryGetValue(vehicle, out result);
        }
    }
}
