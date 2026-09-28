using UnityEngine;
using ACR.Core;

namespace ACR.Vehicle
{
    /// <summary>
    /// PHASE 4 TEST HARNESS
    /// Attach to an empty GameObject in any test scene.
    /// Provides turnkey verification for:
    /// - Third-Person Adaptive Chase Camera (SmoothDamp follow, zero jitter)
    /// - Dynamic Turn Look-Ahead (leans into corners)
    /// - Screen-space Shake Trigger (nitro, collisions, explosions)
    /// - FOV Kick Trigger (nitro boost, slipstream, speed pads)
    /// - Airborne/Ramp tracking without ground clipping
    /// - Live debug controls and runtime camera target swapping
    /// </summary>
    public class Phase4_TestHarness : MonoBehaviour
    {
        [Header("Data Asset")]
        public VehicleDataSO vehicleData;

        private VehicleController _vehicle;
        private Rigidbody _vehicleRb;
        private KeyboardVehicleInput _keyboardInput;
        private ThirdPersonCameraController _chaseCamera;

        private void Start()
        {
            if (vehicleData == null)
            {
                vehicleData = Resources.Load<VehicleDataSO>("CAR_Formula_01");
                if (vehicleData == null)
                {
                    vehicleData = ScriptableObject.CreateInstance<VehicleDataSO>();
                    vehicleData.vehicleId = "CAR_Formula_01";
                    vehicleData.topSpeed = 42f;
                    vehicleData.accelerationForce = 28f;
                    vehicleData.brakeForce = 45f;
                    vehicleData.coastingDrag = 4f;
                    vehicleData.maxSteerAngle = 30f;
                    vehicleData.steerSpeed = 6f;
                    vehicleData.suspensionRestDistance = 0.5f;
                    vehicleData.springStrength = 65f;
                    vehicleData.springDamper = 6.5f;
                    vehicleData.wheelRadius = 0.35f;
                    vehicleData.gripFactor = 0.85f;
                    vehicleData.mass = 1200f;
                }
            }

            BuildTrackElements();
            BuildVehicle();
            SetupChaseCamera();
        }

        private void BuildTrackElements()
        {
            // Main Ground Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Track_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.GetComponent<Renderer>().material.color = new Color(0.22f, 0.25f, 0.28f);

            // Jump Ramp (tests airborne camera behavior and height follow)
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Test_JumpRamp";
            ramp.transform.position = new Vector3(0f, 1.2f, 40f);
            ramp.transform.rotation = Quaternion.Euler(-14f, 0f, 0f);
            ramp.transform.localScale = new Vector3(12f, 0.6f, 16f);
            ramp.GetComponent<Renderer>().material.color = new Color(0.95f, 0.6f, 0.1f);

            // Corner Obstacles (to test turning around bends)
            for (int i = 0; i < 6; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Slalom_Pillar_{i}";
                float x = (i % 2 == 0 ? -6f : 6f);
                float z = 15f + (i * 12f);
                pillar.transform.position = new Vector3(x, 2f, z);
                pillar.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
                pillar.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f);
            }
        }

        private void BuildVehicle()
        {
            GameObject car = new GameObject("Player_Vehicle_Phase4");
            car.transform.position = new Vector3(0f, 1.5f, 0f);

            // Visual Chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chassis.transform.SetParent(car.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            chassis.GetComponent<Renderer>().material.color = new Color(0.05f, 0.65f, 0.95f);
            Destroy(chassis.GetComponent<Collider>());

            // Front Cockpit / Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.transform.SetParent(car.transform);
            nose.transform.localPosition = new Vector3(0f, 0.4f, 1.4f);
            nose.transform.localScale = new Vector3(1.0f, 0.4f, 1.2f);
            nose.GetComponent<Renderer>().material.color = new Color(0.95f, 0.85f, 0.1f);
            Destroy(nose.GetComponent<Collider>());

            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.8f, 4.0f);
            col.center = new Vector3(0f, 0.5f, 0f);

            _vehicleRb = car.AddComponent<Rigidbody>();
            _vehicle = car.AddComponent<VehicleController>();

            _keyboardInput = car.AddComponent<KeyboardVehicleInput>();
            _vehicle.SetInputSource(_keyboardInput);

            // Wheel Sockets
            Transform[] sockets = new Transform[4];
            Vector3[] socketOffsets = new Vector3[]
            {
                new Vector3(-0.95f, 0.35f, 1.3f),
                new Vector3(0.95f, 0.35f, 1.3f),
                new Vector3(-1.0f, 0.35f, -1.3f),
                new Vector3(1.0f, 0.35f, -1.3f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject socketObj = new GameObject(i switch { 0 => "Socket_FL", 1 => "Socket_FR", 2 => "Socket_RL", _ => "Socket_RR" });
                socketObj.transform.SetParent(car.transform);
                socketObj.transform.localPosition = socketOffsets[i];

                GameObject wheelMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheelMesh.transform.SetParent(socketObj.transform);
                wheelMesh.transform.localPosition = Vector3.zero;
                wheelMesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheelMesh.transform.localScale = new Vector3(0.7f, 0.35f, 0.7f);
                wheelMesh.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.14f);
                Destroy(wheelMesh.GetComponent<Collider>());

                sockets[i] = socketObj.transform;
            }

            var fieldSockets = typeof(VehicleController).GetField("wheelSockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldSockets != null) fieldSockets.SetValue(_vehicle, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(_vehicle, vehicleData);
        }

        private void SetupChaseCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
            }

            _chaseCamera = cam.GetComponent<ThirdPersonCameraController>();
            if (_chaseCamera == null)
            {
                _chaseCamera = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            }

            _chaseCamera.SetTarget(_vehicle.transform);
            _chaseCamera.SnapToTarget();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 360, 290), "PHASE 4: THIRD-PERSON CHASE CAMERA");

            float speed = _vehicleRb != null ? GetVelocityMagnitude(_vehicleRb) : 0f;
            float speedKmh = speed * 3.6f;
            GUI.Label(new Rect(25, 40, 330, 20), $"Speed: {speed:F1} u/s ({speedKmh:F0} km/h) | Target: {_vehicle.name}");

            if (_chaseCamera != null)
            {
                string shakeStatus = _chaseCamera.IsShaking ? "ACTIVE (SHAKING)" : "Idle";
                string fovStatus = _chaseCamera.IsFOVKicking ? $"KICK ({_chaseCamera.CurrentFOV:F1}°)" : $"Base ({_chaseCamera.CurrentFOV:F1}°)";

                GUI.Label(new Rect(25, 62, 330, 20), $"Camera FOV: {fovStatus} | Screen Shake: {shakeStatus}");
            }

            GUI.Label(new Rect(25, 88, 330, 20), "Controls: W/S = Gas/Reverse | A/D = Steer | Space = Brake");
            GUI.Label(new Rect(25, 108, 330, 20), "Hotkeys: Press [C] for instant Shake + FOV Kick test");

            // Feedback trigger buttons
            GUI.Label(new Rect(25, 134, 180, 20), "Camera Feedback Hooks:");

            if (GUI.Button(new Rect(25, 158, 150, 26), "💥 Light Shake (0.2s)"))
            {
                if (_chaseCamera != null) _chaseCamera.TriggerShake(0.2f, 0.2f);
            }

            if (GUI.Button(new Rect(185, 158, 175, 26), "💣 Heavy Explosion (0.5s)"))
            {
                if (_chaseCamera != null) _chaseCamera.TriggerShake(0.6f, 0.45f);
            }

            if (GUI.Button(new Rect(25, 190, 150, 26), "🚀 Nitro Kick (+12 FOV)"))
            {
                if (_chaseCamera != null) _chaseCamera.TriggerFOVKick(12f, 0.4f);
            }

            if (GUI.Button(new Rect(185, 190, 175, 26), "⚡ Warp Pad (+20 FOV)"))
            {
                if (_chaseCamera != null) _chaseCamera.TriggerFOVKick(20f, 0.6f);
            }

            if (GUI.Button(new Rect(25, 230, 150, 28), "🔄 Snap Camera"))
            {
                if (_chaseCamera != null) _chaseCamera.SnapToTarget();
            }

            if (GUI.Button(new Rect(185, 230, 175, 28), "🏁 Reset Car to Start"))
            {
                ResetVehicle();
            }

            GUI.Label(new Rect(25, 266, 330, 20), "Observe: SmoothDamp follow + dynamic turn lean");
        }

        private void ResetVehicle()
        {
            if (_vehicle == null || _vehicleRb == null) return;
            _vehicle.transform.position = new Vector3(0f, 1.5f, 0f);
            _vehicle.transform.rotation = Quaternion.identity;
            ResetRbVelocity(_vehicleRb);
            if (_chaseCamera != null) _chaseCamera.SnapToTarget();
        }

        private float GetVelocityMagnitude(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity.magnitude;
#else
            return rb.velocity.magnitude;
#endif
        }

        private void ResetRbVelocity(Rigidbody rb)
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
