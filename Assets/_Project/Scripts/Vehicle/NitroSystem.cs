using UnityEngine;
using ACR.Core;

namespace ACR.Vehicle
{
    /// <summary>
    /// Nitro capacity, depletion, and recharge for one vehicle. Deliberately has no FixedUpdate/Update
    /// of its own — VehicleController is already the single place reading IVehicleInputSource each
    /// physics step, so it calls Tick() directly and gets the boosting state back synchronously,
    /// in the same step it uses that state to compute boosted top speed/acceleration. Two independent
    /// FixedUpdates here would introduce a one-frame lag depending on component execution order;
    /// this avoids that entirely.
    ///
    /// Serves Pillars: "Always a Comeback" & "One-Thumb Mastery".
    /// </summary>
    public class NitroSystem : MonoBehaviour
    {
        [Header("Data (assign the same VehicleDataSO as this vehicle's VehicleController)")]
        [SerializeField] private VehicleDataSO vehicleData;

        [Header("Feedback (optional — assign only on the player vehicle)")]
        [SerializeField] private ThirdPersonCameraController cameraController;

        public float CurrentNitro { get; private set; }
        public float MaxNitro => vehicleData != null ? vehicleData.nitroCapacity : 0f;
        public bool IsBoosting { get; private set; }

        public void Configure(VehicleDataSO data, ThirdPersonCameraController cam = null)
        {
            vehicleData = data;
            cameraController = cam;
            if (vehicleData != null) CurrentNitro = vehicleData.nitroCapacity;
        }

        private void Awake()
        {
            if (vehicleData != null) CurrentNitro = vehicleData.nitroCapacity;
        }

        /// <summary>
        /// Called once per FixedUpdate by VehicleController. Advances depletion/recharge and returns
        /// whether boost is active right now, so the caller can apply the speed multiplier in the
        /// same physics step — no lag, no cross-component ordering assumptions.
        /// </summary>
        public bool Tick(bool requested, float deltaTime)
        {
            if (vehicleData == null) return false;

            bool wasBoosting = IsBoosting;
            bool wantsNitro = requested && CurrentNitro > 0f;

            if (wantsNitro)
            {
                IsBoosting = true;
                CurrentNitro = Mathf.Max(0f, CurrentNitro - vehicleData.nitroDepletionRate * deltaTime);
            }
            else
            {
                IsBoosting = false;
                CurrentNitro = Mathf.Min(vehicleData.nitroCapacity, CurrentNitro + vehicleData.nitroRechargeRate * deltaTime);
            }

            if (IsBoosting && !wasBoosting && cameraController != null)
            {
                cameraController.TriggerFOVKick(8f, 0.4f);
            }

            return IsBoosting;
        }
    }
}
