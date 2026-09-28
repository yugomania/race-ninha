using System;
using System.Collections.Generic;
using UnityEngine;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// Result of the player's race, handed to UI/results systems (Phase 16 builds the real screen).
    /// </summary>
    public class RaceResult
    {
        public VehicleController vehicle;
        public bool won;
        public int place;
        public float finishTime;
    }

    /// <summary>
    /// Determines when the PLAYER's race is over and whether they won. Concludes the instant the
    /// player crosses the finish line on their final lap — matches how arcade racers universally
    /// work (the player's race ends on their own finish, not on last place finishing). AI vehicles
    /// finishing later still get recorded in finishOrder for placement purposes but don't block
    /// the player's result.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        [SerializeField] private VehicleController playerVehicle;

        public static event Action<RaceResult> OnRaceCompleted;

        private readonly List<VehicleController> finishOrder = new List<VehicleController>();
        private bool raceConcludedForPlayer;

        public VehicleController PlayerVehicle => playerVehicle;
        public bool RaceConcludedForPlayer => raceConcludedForPlayer;
        public IReadOnlyList<VehicleController> FinishOrder => finishOrder;

        public void Configure(VehicleController vehicle)
        {
            playerVehicle = vehicle;
        }

        private void OnEnable()
        {
            LapSystem.OnVehicleFinished += HandleVehicleFinished;
            RaceCountdownController.OnStagingComplete += ResetForRestart;
        }

        private void OnDisable()
        {
            LapSystem.OnVehicleFinished -= HandleVehicleFinished;
            RaceCountdownController.OnStagingComplete -= ResetForRestart;
        }

        public void ResetForRestart()
        {
            finishOrder.Clear();
            raceConcludedForPlayer = false;
        }

        private void HandleVehicleFinished(VehicleController vehicle, float finishTime)
        {
            if (finishOrder.Contains(vehicle)) return; // Safety: LapSystem only fires once per vehicle

            finishOrder.Add(vehicle);
            int place = finishOrder.Count;

            if (vehicle == playerVehicle && !raceConcludedForPlayer)
            {
                raceConcludedForPlayer = true;
                OnRaceCompleted?.Invoke(new RaceResult
                {
                    vehicle = vehicle,
                    won = place == 1,
                    place = place,
                    finishTime = finishTime
                });
            }
        }
    }
}
