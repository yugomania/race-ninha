using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Combat
{
    /// <summary>
    /// PHASE 10 TEST HARNESS
    /// Turnkey validation for the first fictional weapon (Homing Missile) and generic ObjectPool.
    /// Demonstrates:
    /// - Zero GC allocations during combat: 5 prewarmed missile instances reused continuously.
    /// - Arcade homing trajectory: steers towards target vehicle with limited turn rate.
    /// - Cooldown gating: weapon cannot fire until cooldown elapses.
    /// - Hit detection: logs [Phase10 stub] on impact and returns to pool cleanly.
    /// - Countdown lock respect: firing blocked while controls are locked.
    /// </summary>
    public class Phase10_TestHarness : MonoBehaviour
    {
        [Header("Weapon References")]
        [SerializeField] private HomingMissileWeapon playerWeapon;
        [SerializeField] private VehicleController playerVehicle;
        [SerializeField] private TouchVehicleInput playerTouchInput;
        [SerializeField] private VehicleController targetVehicle;

        [Header("Vehicle Config")]
        public VehicleDataSO vehicleData;

        private void Start()
        {
            if (playerWeapon == null)
            {
                AutoSetupTestEnvironment();
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
                }
            }

            // Create Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Track_CombatGround";
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

            ThirdPersonCameraController chaseCam = cam.GetComponent<ThirdPersonCameraController>();
            if (chaseCam == null)
            {
                chaseCam = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            }

            // Build Procedural Missile Prefab (inactive template in scene)
            GameObject missileTemplate = new GameObject("Prefab_HomingMissile_01");
            missileTemplate.transform.position = new Vector3(0f, -50f, 0f); // Hidden far below

            GameObject missileMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missileMesh.transform.SetParent(missileTemplate.transform);
            missileMesh.transform.localPosition = Vector3.zero;
            missileMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            missileMesh.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            missileMesh.GetComponent<Renderer>().material.color = new Color(1f, 0.2f, 0.1f);
            Destroy(missileMesh.GetComponent<Collider>());

            SphereCollider sc = missileTemplate.AddComponent<SphereCollider>();
            sc.radius = 0.6f;
            sc.isTrigger = true;

            HomingMissileProjectile projScript = missileTemplate.AddComponent<HomingMissileProjectile>();
            missileTemplate.SetActive(false); // Template is disabled

            // Build Player Vehicle
            GameObject pCar = new GameObject("Player_Combat_Vehicle");
            pCar.transform.position = new Vector3(0f, 1.5f, 0f);

            GameObject pChassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pChassis.transform.SetParent(pCar.transform);
            pChassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pChassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pChassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            pChassis.GetComponent<Renderer>().material.color = new Color(0.05f, 0.7f, 0.95f);
            Destroy(pChassis.GetComponent<Collider>());

            BoxCollider pCol = pCar.AddComponent<BoxCollider>();
            pCol.size = new Vector3(1.8f, 0.8f, 4.0f);
            pCol.center = new Vector3(0f, 0.5f, 0f);

            pCar.AddComponent<Rigidbody>();
            playerVehicle = pCar.AddComponent<VehicleController>();

            KeyboardVehicleInput keyInput = pCar.AddComponent<KeyboardVehicleInput>();
            playerTouchInput = pCar.AddComponent<TouchVehicleInput>();
            playerVehicle.SetInputSource(keyInput);

            // Wheel Sockets
            SetupWheelSockets(pCar, playerVehicle);

            // Fire Point on front nose of player vehicle
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(pCar.transform);
            fp.transform.localPosition = new Vector3(0f, 0.6f, 2.2f);
            fp.transform.localRotation = Quaternion.identity;

            // Attach HomingMissileWeapon
            playerWeapon = pCar.AddComponent<HomingMissileWeapon>();
            playerWeapon.Configure(projScript, fp.transform, 2.0f);

            // Build Target Opponent Vehicle 25m ahead
            GameObject tCar = new GameObject("Target_Opponent_Dummy");
            tCar.transform.position = new Vector3(4f, 1.5f, 25f);

            GameObject tChassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tChassis.transform.SetParent(tCar.transform);
            tChassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            tChassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tChassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            tChassis.GetComponent<Renderer>().material.color = new Color(0.95f, 0.2f, 0.2f); // Red
            Destroy(tChassis.GetComponent<Collider>());

            BoxCollider tCol = tCar.AddComponent<BoxCollider>();
            tCol.size = new Vector3(1.8f, 0.8f, 4.0f);
            tCol.center = new Vector3(0f, 0.5f, 0f);

            tCar.AddComponent<Rigidbody>();
            targetVehicle = tCar.AddComponent<VehicleController>();
            SetupWheelSockets(tCar, targetVehicle);

            // Camera Target
            chaseCam.SetTarget(pCar.transform);
            chaseCam.SnapToTarget();
        }

        private void SetupWheelSockets(GameObject car, VehicleController vc)
        {
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
            if (fieldSockets != null) fieldSockets.SetValue(vc, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(vc, vehicleData);
        }

        private void OnGUI()
        {
            DrawWeaponHUD();

            if (playerTouchInput != null)
            {
                DrawTouchFireButton();
            }
        }

        private void DrawWeaponHUD()
        {
            GUI.Box(new Rect(10, 10, 390, 220), "PHASE 10: HOMING MISSILE & OBJECT POOL");

            string statusText = (playerWeapon != null && playerWeapon.IsReady)
                ? "🟢 WEAPON READY"
                : $"🔴 COOLDOWN ({playerWeapon?.CooldownRemaining:F1}s)";
            GUI.Label(new Rect(20, 34, 350, 20), $"Weapon Status: {statusText}");

            if (playerWeapon != null)
            {
                float ratio = playerWeapon.CooldownDuration > 0f
                    ? 1f - Mathf.Clamp01(playerWeapon.CooldownRemaining / playerWeapon.CooldownDuration)
                    : 1f;

                Rect bg = new Rect(20, 56, 320, 20);
                GUI.Box(bg, "");
                Rect fill = new Rect(bg.x + 2, bg.y + 2, (bg.width - 4) * ratio, bg.height - 4);

                Color prevColor = GUI.color;
                GUI.color = playerWeapon.IsReady ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.6f, 0.1f);
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = prevColor;
            }

            int availableCount = playerWeapon != null ? playerWeapon.AvailableMissiles : 0;
            GUI.Label(new Rect(20, 80, 350, 20), $"Object Pool: {availableCount} instances in reserve (Zero GC Alloc)");

            string targetInfo = targetVehicle != null
                ? $"{targetVehicle.name} ({Vector3.Distance(playerVehicle.transform.position, targetVehicle.transform.position):F1}m away)"
                : "None in range";
            GUI.Label(new Rect(20, 100, 350, 20), $"Nearest Target: {targetInfo}");

            GUI.Label(new Rect(20, 126, 350, 20), "Keyboard: Press [Left Ctrl] to Fire");
            GUI.Label(new Rect(20, 146, 350, 20), "Touch: Tap [FIRE 🚀] Button (bottom-right)");

            if (GUI.Button(new Rect(20, 174, 160, 26), "Reset Target Position"))
            {
                if (targetVehicle != null)
                {
                    targetVehicle.transform.position = new Vector3(Random.Range(-8f, 8f), 1.5f, Random.Range(20f, 35f));
                    targetVehicle.transform.rotation = Quaternion.identity;
                }
            }
        }

        private void DrawTouchFireButton()
        {
            Rect buttonRect = new Rect(Screen.width - 160, Screen.height - 160, 130, 130);
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.3f, 0.2f) }
            };

            bool isHeld = GUI.RepeatButton(buttonRect, "FIRE\n🚀", btnStyle);
            if (playerTouchInput != null)
            {
                playerTouchInput.SetWeaponButtonHeld(isHeld);
            }
        }
    }
}
