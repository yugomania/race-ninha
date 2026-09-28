using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// Keyboard/gamepad-axis input source. Used in the editor and on any desktop test builds.
    /// This is what the temporary debug block in VehicleController (Phase 2) is being replaced by.
    /// </summary>
    public class KeyboardVehicleInput : MonoBehaviour, IVehicleInputSource
    {
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Brake { get; private set; }
        public bool NitroRequested { get; private set; }
        public bool WeaponFireRequested { get; private set; }

        private void Update()
        {
            Throttle = Input.GetAxis("Vertical");
            Steer = Input.GetAxis("Horizontal");
            Brake = Input.GetKey(KeyCode.Space);
            NitroRequested = Input.GetKey(KeyCode.LeftShift);
            WeaponFireRequested = Input.GetKey(KeyCode.LeftControl);
        }
    }
}
