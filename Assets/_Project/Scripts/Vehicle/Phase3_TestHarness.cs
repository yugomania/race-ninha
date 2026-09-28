using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// PHASE 3 TEST HARNESS
    /// Attach to an empty GameObject in any test scene.
    /// Provides turnkey verification for:
    /// - Input Decoupling (IVehicleInputSource)
    /// - Keyboard/Gamepad Driving (KeyboardVehicleInput)
    /// - Mobile Screen-Zone Touch Driving (TouchVehicleInput)
    /// - Live Toggles for Auto-Acceleration and Steer-Assist
    /// </summary>
    public class Phase3_TestHarness : MonoBehaviour
    {
        [Header("Data Asset")]
        public VehicleDataSO vehicleData;

        [Header("Active Input Mode")]
        public bool useTouchInput = false;

        private VehicleController _vehicle;
        private Rigidbody _vehicleRb;
        private KeyboardVehicleInput _keyboardInput;
        private TouchVehicleInput _touchInput;

        private void Start()
        {
            if (vehicleData == null)
            {
                vehicleData = Resources.Load<VehicleDataSO>("CAR_Formula_01");
                if (vehicleData == null)
                {
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

            GameObject bump = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bump.name = "Test_Bump";
            bump.transform.position = new Vector3(0f, 0.2f, 25f);
            bump.transform.localScale = new Vector3(8f, 0.4f, 4f);
            bump.GetComponent<Renderer>().material.color = new Color(0.85f, 0.55f, 0.15f);
        }

        private void BuildVehicle()
        {
            GameObject car = new GameObject("Player_Vehicle_Phase3");
            car.transform.position = new Vector3(0f, 1.5f, 0f);

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chassis.transform.SetParent(car.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            chassis.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 0.9f);
            Destroy(chassis.GetComponent<Collider>());

            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.8f, 4.0f);
            col.center = new Vector3(0f, 0.5f, 0f);

            _vehicleRb = car.AddComponent<Rigidbody>();
            _vehicle = car.AddComponent<VehicleController>();

            // Attach both input components
            _keyboardInput = car.AddComponent<KeyboardVehicleInput>();
            _touchInput = car.AddComponent<TouchVehicleInput>();

            // Setup 4 Wheel Sockets
            Transform[] sockets = new Transform[4];
            Vector3[] socketOffsets = new Vector3[]
            {
                new Vector3(-0.9f, 0.35f, 1.3f),
                new Vector3(0.9f, 0.35f, 1.3f),
                new Vector3(-0.95f, 0.35f, -1.3f),
                new Vector3(0.95f, 0.35f, -1.3f)
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
                wheelMesh.GetComponent<Renderer>().material.color = new Color(0.1f, 0.12f, 0.15f);
                Destroy(wheelMesh.GetComponent<Collider>());

                sockets[i] = socketObj.transform;
            }

            var fieldSockets = typeof(VehicleController).GetField("wheelSockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldSockets != null) fieldSockets.SetValue(_vehicle, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(_vehicle, vehicleData);

            // Bind active input source
            SwitchInputSource(useTouchInput);
        }

        private void SwitchInputSource(bool touch)
        {
            useTouchInput = touch;
            if (_vehicle != null)
            {
                _vehicle.SetInputSource(useTouchInput ? _touchInput : _keyboardInput);
            }
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

            var follower = cam.gameObject.AddComponent<TestCameraFollow>();
            follower.target = _vehicle.transform;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 340, 240), "PHASE 3: CONTROLS & TOUCH ASSISTS");

            float currentSpeed = _vehicleRb != null ? GetVelocityMagnitude(_vehicleRb) : 0f;
            GUI.Label(new Rect(25, 42, 320, 20), $"Speed: {currentSpeed:F1} units/s (Max: {vehicleData.topSpeed})");

            IVehicleInputSource activeSource = _vehicle != null ? _vehicle.InputSource : null;
            if (activeSource != null)
            {
                GUI.Label(new Rect(25, 62, 320, 20), $"Input [Throttle: {activeSource.Throttle:F2} | Steer: {activeSource.Steer:F2} | Brake: {activeSource.Brake}]");
            }

            // Input Mode Switcher
            GUI.Label(new Rect(25, 88, 120, 20), "Active Source:");
            if (GUI.Button(new Rect(135, 88, 100, 22), useTouchInput ? "📱 Mobile Touch" : "⌨️ Keyboard"))
            {
                SwitchInputSource(!useTouchInput);
            }

            if (useTouchInput && _touchInput != null)
            {
                _touchInput.autoAccelerate = GUI.Toggle(new Rect(25, 115, 290, 20), _touchInput.autoAccelerate, " Assist: Auto-Accelerate");
                _touchInput.steerAssist = GUI.Toggle(new Rect(25, 135, 290, 20), _touchInput.steerAssist, " Assist: Steer Auto-Recenter");
                GUI.Label(new Rect(25, 155, 300, 20), "Touch: Left Half = Steer Drag | Right = Gas");
            }
            else
            {
                GUI.Label(new Rect(25, 115, 300, 20), "Keys: W/S (Gas/Brake) | A/D (Steer) | Space (Brake)");
            }

            if (GUI.Button(new Rect(25, 195, 120, 25), "Reset Position"))
            {
                _vehicle.transform.position = new Vector3(0f, 1.5f, 0f);
                _vehicle.transform.rotation = Quaternion.identity;
                ResetRbVelocity(_vehicleRb);
            }
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
