using UnityEngine;
using ACR.Core;

namespace ACR.Vehicle
{
    /// <summary>
    /// Disposable Phase 9 scaffolding: shows the player's nitro meter and, on touch/mobile builds,
    /// a press-and-hold nitro button that calls TouchVehicleInput.SetNitroButtonHeld — the exact
    /// method Phase 15's real HUD button will call.
    /// On keyboard, nitro is testable via Left Shift.
    ///
    /// Turnkey setup: if playerNitroSystem is not assigned in the inspector, Start() will automatically
    /// construct a test track circuit, player vehicle with NitroSystem, chase camera with FOV kick,
    /// and input components.
    /// </summary>
    public class Phase9_TestHarness : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NitroSystem playerNitroSystem;
        [Tooltip("Optional — only needed if testing the touch input path in the editor/on device.")]
        [SerializeField] private TouchVehicleInput playerTouchInput;
        [SerializeField] private VehicleController playerVehicle;
        [SerializeField] private ThirdPersonCameraController chaseCamera;

        [Header("Vehicle Config")]
        public VehicleDataSO vehicleData;

        private Rigidbody playerRb;

        private void Start()
        {
            if (playerNitroSystem == null)
            {
                AutoSetupTestEnvironment();
            }
            if (playerVehicle != null)
            {
                playerRb = playerVehicle.GetComponent<Rigidbody>();
            }
        }

        private void AutoSetupTestEnvironment()
        {
            if (vehicleData == null)
            {
                vehicleData = Resources.Load<VehicleDataSO>("CAR_Formula_01");
                if (vehicleData == null)
                {
                    vehicleData = ScriptableObject.CreateInstance<VehicleDataSO>();
                    vehicleData.vehicleId = "CAR_Formula_01";
                    vehicleData.topSpeed = 40f;
                    vehicleData.accelerationForce = 26f;
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
                    vehicleData.nitroCapacity = 100f;
                    vehicleData.nitroRechargeRate = 8f;
                    vehicleData.nitroDepletionRate = 25f;
                    vehicleData.nitroBoostMultiplier = 1.4f;
                }
            }

            // Create Track Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Track_NitroTestPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().material.color = new Color(0.22f, 0.25f, 0.28f);

            // Create Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
            }

            chaseCamera = cam.GetComponent<ThirdPersonCameraController>();
            if (chaseCamera == null)
            {
                chaseCamera = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            }

            // Create Vehicle
            GameObject car = new GameObject("Player_Nitro_Vehicle");
            car.transform.position = new Vector3(0f, 1.5f, 0f);

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chassis.transform.SetParent(car.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            chassis.GetComponent<Renderer>().material.color = new Color(0.05f, 0.7f, 0.95f);
            Destroy(chassis.GetComponent<Collider>());

            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.8f, 4.0f);
            col.center = new Vector3(0f, 0.5f, 0f);

            playerRb = car.AddComponent<Rigidbody>();

            // Attach NitroSystem first so VehicleController finds it in Awake
            playerNitroSystem = car.AddComponent<NitroSystem>();
            playerNitroSystem.Configure(vehicleData, chaseCamera);

            playerVehicle = car.AddComponent<VehicleController>();

            KeyboardVehicleInput keyInput = car.AddComponent<KeyboardVehicleInput>();
            playerTouchInput = car.AddComponent<TouchVehicleInput>();
            playerVehicle.SetInputSource(keyInput);

            // Wheel Sockets
            Transform[] sockets = new Transform[4];
            Vector3[] socketOffsets = new Vector3[]
            {
                new Vector3(-0.95f, 0.35f, 1.3f),
                new Vector3(0.95f, 0.35f, 1.3f),
                new Vector3(-1.0f, 0.35f, -1.3f),
                new Vector3(1.0f, 0.35f, -1.3f)
            };

            for (int s = 0; s < 4; s++)
            {
                GameObject socketObj = new GameObject(s switch { 0 => "Socket_FL", 1 => "Socket_FR", 2 => "Socket_RL", _ => "Socket_RR" });
                socketObj.transform.SetParent(car.transform);
                socketObj.transform.localPosition = socketOffsets[s];

                GameObject wheelMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheelMesh.transform.SetParent(socketObj.transform);
                wheelMesh.transform.localPosition = Vector3.zero;
                wheelMesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheelMesh.transform.localScale = new Vector3(0.7f, 0.35f, 0.7f);
                wheelMesh.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.14f);
                Destroy(wheelMesh.GetComponent<Collider>());

                sockets[s] = socketObj.transform;
            }

            var fieldSockets = typeof(VehicleController).GetField("wheelSockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldSockets != null) fieldSockets.SetValue(playerVehicle, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(playerVehicle, vehicleData);

            chaseCamera.SetTarget(car.transform);
            chaseCamera.SnapToTarget();
        }

        private void OnGUI()
        {
            DrawNitroHUD();

            if (playerTouchInput != null)
            {
                DrawNitroButton();
            }
        }

        private void DrawNitroHUD()
        {
            GUI.Box(new Rect(10, 10, 360, 190), "PHASE 9: NITRO BOOST SYSTEM");

            float speed = 0f;
            if (playerRb != null)
            {
#if UNITY_6000_0_OR_NEWER
                speed = playerRb.linearVelocity.magnitude;
#else
                speed = playerRb.velocity.magnitude;
#endif
            }

            float baseTop = vehicleData != null ? vehicleData.topSpeed : 40f;
            float boostedTop = baseTop * (vehicleData != null ? vehicleData.nitroBoostMultiplier : 1.4f);

            GUI.Label(new Rect(20, 32, 340, 20), $"Speed: {speed:F1} u/s (Base: {baseTop:F0} | Boost: {boostedTop:F0})");

            float fov = chaseCamera != null ? chaseCamera.CurrentFOV : 60f;
            string boostState = playerNitroSystem != null && playerNitroSystem.IsBoosting ? "🔥 BOOSTING (FOV Kick Active)" : "Idle";
            GUI.Label(new Rect(20, 52, 340, 20), $"Status: {boostState} | FOV: {fov:F1}°");

            // Nitro Bar
            if (playerNitroSystem != null)
            {
                float fillRatio = playerNitroSystem.MaxNitro > 0f ? playerNitroSystem.CurrentNitro / playerNitroSystem.MaxNitro : 0f;

                Rect background = new Rect(20, 78, 300, 26);
                GUI.Box(background, "");

                Rect fill = new Rect(background.x + 2, background.y + 2, (background.width - 4) * fillRatio, background.height - 4);
                Color prevColor = GUI.color;
                GUI.color = playerNitroSystem.IsBoosting ? new Color(0f, 0.9f, 1f) : new Color(1f, 0.85f, 0.1f);
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = prevColor;

                GUIStyle label = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.black }
                };
                GUI.Label(background, $"NITRO {playerNitroSystem.CurrentNitro:F0} / {playerNitroSystem.MaxNitro:F0}", label);
            }

            GUI.Label(new Rect(20, 114, 340, 20), "Keyboard: Hold [Left Shift] to Boost");
            GUI.Label(new Rect(20, 134, 340, 20), "Touch: Hold [NITRO] Button (bottom-right)");

            if (GUI.Button(new Rect(20, 158, 140, 26), "Reset Car Position"))
            {
                if (playerVehicle != null)
                {
                    playerVehicle.transform.position = new Vector3(0f, 1.5f, 0f);
                    playerVehicle.transform.rotation = Quaternion.identity;
                    if (playerRb != null)
                    {
#if UNITY_6000_0_OR_NEWER
                        playerRb.linearVelocity = Vector3.zero;
#else
                        playerRb.velocity = Vector3.zero;
#endif
                        playerRb.angularVelocity = Vector3.zero;
                    }
                    if (chaseCamera != null) chaseCamera.SnapToTarget();
                }
            }
        }

        private void DrawNitroButton()
        {
            Rect buttonRect = new Rect(Screen.width - 160, Screen.height - 160, 130, 130);
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            bool isHeld = GUI.RepeatButton(buttonRect, "NITRO\n🚀", btnStyle);
            if (playerTouchInput != null)
            {
                playerTouchInput.SetNitroButtonHeld(isHeld);
            }
        }
    }
}
