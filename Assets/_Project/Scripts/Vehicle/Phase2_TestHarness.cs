using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// PHASE 2 TEST HARNESS
    /// Attach this script to an empty GameObject in any blank Unity scene.
    /// In Play Mode, it automatically builds:
    /// - Flat Ground Plane with collision
    /// - Complete Vehicle with Rigidbody, BoxCollider, and 4 Wheel Sockets (FL, FR, RL, RR)
    /// - Binds CAR_Formula_01.asset
    /// - Overhead Camera
    /// - Live On-Screen Telemetry GUI (Speed, Wheel Grounding, Steer Angle)
    /// </summary>
    public class Phase2_TestHarness : MonoBehaviour
    {
        [Header("Data Asset")]
        public VehicleDataSO vehicleData;

        private VehicleController _vehicle;
        private Rigidbody _vehicleRb;

        private void Start()
        {
            if (vehicleData == null)
            {
                vehicleData = Resources.Load<VehicleDataSO>("CAR_Formula_01");
                if (vehicleData == null)
                {
                    // Fallback runtime asset creation if not assigned
                    vehicleData = ScriptableObject.CreateInstance<VehicleDataSO>();
                    vehicleData.vehicleId = "CAR_Formula_01";
                    vehicleData.topSpeed = 40f;
                    vehicleData.accelerationForce = 25f;
                    vehicleData.brakeForce = 40f;
                    vehicleData.coastingDrag = 4f;
                    vehicleData.maxSteerAngle = 30f;
                    vehicleData.steerSpeed = 5f;
                    vehicleData.suspensionRestDistance = 0.5f;
                    vehicleData.springStrength = 60f;
                    vehicleData.springDamper = 6f;
                    vehicleData.wheelRadius = 0.35f;
                    vehicleData.gripFactor = 0.85f;
                    vehicleData.mass = 1200f;
                }
            }

            BuildGround();
            BuildVehicle();
            SetupCamera();
        }

        private void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().material.color = new Color(0.25f, 0.28f, 0.32f);

            // Small test bump/ramp to test suspension compression
            GameObject bump = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bump.name = "Test_Bump";
            bump.transform.position = new Vector3(0f, 0.2f, 25f);
            bump.transform.localScale = new Vector3(8f, 0.4f, 4f);
            bump.GetComponent<Renderer>().material.color = new Color(0.85f, 0.55f, 0.15f);
        }

        private void BuildVehicle()
        {
            GameObject car = new GameObject("Player_Vehicle_Phase2");
            car.transform.position = new Vector3(0f, 1.5f, 0f);

            // Visual Chassis (Capsule proxy)
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chassis.transform.SetParent(car.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            chassis.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 0.9f);
            Destroy(chassis.GetComponent<Collider>()); // Use parent box collider

            // Physical BoxCollider
            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.8f, 4.0f);
            col.center = new Vector3(0f, 0.5f, 0f);

            _vehicleRb = car.AddComponent<Rigidbody>();
            _vehicle = car.AddComponent<VehicleController>();

            // Setup 4 Wheel Sockets
            Transform[] sockets = new Transform[4];
            Vector3[] socketOffsets = new Vector3[]
            {
                new Vector3(-0.9f, 0.35f, 1.3f),   // FL
                new Vector3(0.9f, 0.35f, 1.3f),    // FR
                new Vector3(-0.95f, 0.35f, -1.3f), // RL
                new Vector3(0.95f, 0.35f, -1.3f)  // RR
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject socketObj = new GameObject(i switch { 0 => "Socket_FL", 1 => "Socket_FR", 2 => "Socket_RL", _ => "Socket_RR" });
                socketObj.transform.SetParent(car.transform);
                socketObj.transform.localPosition = socketOffsets[i];

                // Visual cylinder wheel proxy
                GameObject wheelMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheelMesh.transform.SetParent(socketObj.transform);
                wheelMesh.transform.localPosition = Vector3.zero;
                wheelMesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheelMesh.transform.localScale = new Vector3(0.7f, 0.35f, 0.7f);
                wheelMesh.GetComponent<Renderer>().material.color = new Color(0.1f, 0.12f, 0.15f);
                Destroy(wheelMesh.GetComponent<Collider>()); // Pure visual, raycast handles physics

                sockets[i] = socketObj.transform;
            }

            // Assign private fields via serialized properties / reflection helper
            var fieldSockets = typeof(VehicleController).GetField("wheelSockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldSockets != null) fieldSockets.SetValue(_vehicle, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(_vehicle, vehicleData);

            var fieldSteerCount = typeof(VehicleController).GetField("steeringWheelCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldSteerCount != null) fieldSteerCount.SetValue(_vehicle, 2);
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Test_Camera");
                cam = camObj.AddComponent<Camera>();
            }

            cam.transform.position = new Vector3(0f, 6f, -10f);
            cam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

            // Simple camera follower
            var follower = cam.gameObject.AddComponent<TestCameraFollow>();
            follower.target = _vehicle.transform;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 320, 170), "PHASE 2 CONFIRMATION BENCH");

            float currentSpeed = _vehicleRb != null ? _vehicleRb.linearVelocity.magnitude : 0f;
            GUI.Label(new Rect(25, 45, 300, 20), $"Current Speed: {currentSpeed:F1} units/s (Max: {vehicleData.topSpeed})");
            GUI.Label(new Rect(25, 65, 300, 20), $"Vehicle Mass: {vehicleData.mass} kg");

            int groundedCount = 0;
            if (_vehicle != null && _vehicle.WheelGrounded != null)
            {
                foreach (bool g in _vehicle.WheelGrounded) if (g) groundedCount++;
            }
            GUI.Label(new Rect(25, 85, 300, 20), $"Grounded Wheels: {groundedCount}/4 {(groundedCount == 4 ? "✅ (PASSED)" : "⚠️")}");

            GUI.Label(new Rect(25, 115, 300, 20), "Controls: W/S (Throttle/Brake) | A/D (Steer)");
            GUI.Label(new Rect(25, 135, 300, 20), "SPACE: Handbrake | R: Reset Position");

            if (GUI.Button(new Rect(25, 155, 120, 22), "Reset Position"))
            {
                _vehicle.transform.position = new Vector3(0f, 1.5f, 0f);
                _vehicle.transform.rotation = Quaternion.identity;
                _vehicleRb.linearVelocity = Vector3.zero;
                _vehicleRb.angularVelocity = Vector3.zero;
            }
        }

        private class TestCameraFollow : MonoBehaviour
        {
            public Transform target;
            private void LateUpdate()
            {
                if (target == null) return;
                Vector3 desired = target.position - target.forward * 8f + Vector3.up * 4.5f;
                transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 10f);
                transform.LookAt(target.position + Vector3.up * 1.0f);
            }
        }
    }
}
