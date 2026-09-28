using System.Collections.Generic;
using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// Disposable test harness for Phase 6 — visualizes the countdown, provides audio-beep
    /// placeholders (Phase 22 will replace with real SFX), actively checks that locked vehicles
    /// truly don't move on flat or sloped grids, and offers an instant restart.
    ///
    /// Turnkey setup: if countdownController is not assigned in the inspector, Start()
    /// will automatically construct a circuit with an inclined/sloped starting grid and
    /// 4 race vehicles to thoroughly validate the physics-level parking brake lock.
    /// </summary>
    public class Phase6_TestHarness : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private RaceCountdownController countdownController;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip beepClip;

        [Tooltip("Rigidbody speed above this while locked is treated as a controls-lock failure.")]
        [SerializeField] private float lockViolationThreshold = 0.05f;

        [Header("Vehicle Data")]
        public VehicleDataSO vehicleData;

        private string countdownDisplay = "";
        private bool showingGo;
        private float goDisplayTimer;
        private readonly HashSet<VehicleController> warnedThisSequence = new HashSet<VehicleController>();
        private readonly List<string> validationWarnings = new List<string>();

        private void OnEnable()
        {
            RaceCountdownController.OnCountdownTick += HandleTick;
            RaceCountdownController.OnRaceStarted += HandleRaceStarted;
            RaceCountdownController.OnStagingComplete += HandleStagingComplete;
        }

        private void OnDisable()
        {
            RaceCountdownController.OnCountdownTick -= HandleTick;
            RaceCountdownController.OnRaceStarted -= HandleRaceStarted;
            RaceCountdownController.OnStagingComplete -= HandleStagingComplete;
        }

        private void Start()
        {
            if (countdownController == null)
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

            // Create Track Root
            GameObject trackRoot = new GameObject("Track_Phase6_SlopedGrid");

            // Main Track Ground (inclined at starting grid by 4 degrees to rigorously test brake hold!)
            GameObject roadMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadMain.name = "Road_SlopedGrid";
            roadMain.transform.SetParent(trackRoot.transform);
            roadMain.transform.position = new Vector3(0f, 1.0f, 10f);
            roadMain.transform.rotation = Quaternion.Euler(4.0f, 0f, 0f); // 4-degree slope
            roadMain.transform.localScale = new Vector3(16f, 0.4f, 70f);
            roadMain.GetComponent<Renderer>().material.color = new Color(0.24f, 0.26f, 0.28f);

            // Checkpoints
            GameObject cpRoot = new GameObject("Checkpoints");
            cpRoot.transform.SetParent(trackRoot.transform);

            GameObject cpObj = new GameObject("Checkpoint_00_FinishLine");
            cpObj.transform.SetParent(cpRoot.transform);
            cpObj.transform.position = new Vector3(0f, 2.5f, 20f);
            BoxCollider cpCol = cpObj.AddComponent<BoxCollider>();
            cpCol.size = new Vector3(18f, 6f, 3f);
            cpCol.isTrigger = true;
            TrackCheckpoint cp = cpObj.AddComponent<TrackCheckpoint>();
            cp.checkpointIndex = 0;
            cp.isFinishLine = true;

            // Start Grid (4 slots, placed on the sloped road)
            GameObject gridRoot = new GameObject("StartGrid");
            gridRoot.transform.SetParent(trackRoot.transform);

            Transform[] gridTransforms = new Transform[4];
            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-3.0f, 1.6f, 5f),   // Slot 0 (Pole)
                new Vector3(3.0f, 1.2f, -2f),   // Slot 1
                new Vector3(-3.0f, 0.7f, -9f),  // Slot 2
                new Vector3(3.0f, 0.3f, -16f)   // Slot 3
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject slot = new GameObject($"GridSlot_{i}");
                slot.transform.SetParent(gridRoot.transform);
                slot.transform.position = gridOffsets[i];
                slot.transform.rotation = Quaternion.Euler(4.0f, 0f, 0f); // Aligned to slope
                gridTransforms[i] = slot.transform;
            }

            TrackManager tm = trackRoot.AddComponent<TrackManager>();
            tm.SetCheckpoints(new TrackCheckpoint[] { cp });
            tm.SetStartGridPositions(gridTransforms);

            // Build 4 Vehicles (1 Player + 3 AI Staging Dummies)
            VehicleController[] racerArray = new VehicleController[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject car = new GameObject(i == 0 ? "Player_Vehicle_P1" : $"Opponent_Vehicle_P{i + 1}");
                car.transform.position = gridTransforms[i].position + Vector3.up * 0.8f;
                car.transform.rotation = gridTransforms[i].rotation;

                GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                chassis.transform.SetParent(car.transform);
                chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
                chassis.GetComponent<Renderer>().material.color = i switch
                {
                    0 => new Color(0.05f, 0.7f, 0.95f), // Cyan Player
                    1 => new Color(0.95f, 0.2f, 0.2f),  // Red Opponent
                    2 => new Color(0.95f, 0.75f, 0.1f), // Yellow Opponent
                    _ => new Color(0.2f, 0.85f, 0.3f)   // Green Opponent
                };
                Destroy(chassis.GetComponent<Collider>());

                BoxCollider col = car.AddComponent<BoxCollider>();
                col.size = new Vector3(1.8f, 0.8f, 4.0f);
                col.center = new Vector3(0f, 0.5f, 0f);

                Rigidbody rb = car.AddComponent<Rigidbody>();
                VehicleController vc = car.AddComponent<VehicleController>();

                if (i == 0)
                {
                    KeyboardVehicleInput keyInput = car.AddComponent<KeyboardVehicleInput>();
                    vc.SetInputSource(keyInput);
                }

                // Sockets
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

                racerArray[i] = vc;
            }

            // Setup Camera
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

            // Attach Countdown Controller
            GameObject countdownObj = new GameObject("RaceCountdownController");
            countdownController = countdownObj.AddComponent<RaceCountdownController>();
            countdownController.Configure(tm, racerArray, chaseCam, 0);
        }

        private void HandleStagingComplete()
        {
            warnedThisSequence.Clear();
            validationWarnings.Clear();
        }

        private void HandleTick(int count)
        {
            countdownDisplay = count.ToString();
            PlayBeep();
        }

        private void HandleRaceStarted()
        {
            countdownDisplay = "GO!";
            showingGo = true;
            goDisplayTimer = 1.2f;
            PlayBeep();
        }

        private void PlayBeep()
        {
            if (audioSource != null && beepClip != null)
            {
                audioSource.PlayOneShot(beepClip);
            }
            else
            {
                Debug.Log("[Phase6 beep placeholder — real SFX arrives in Phase 22]");
            }
        }

        private void FixedUpdate()
        {
            // Controls-lock validation: while Staging or CountingDown, no racer should be moving.
            if (countdownController == null) return;
            if (countdownController.State == CountdownState.Racing) return;

            VehicleController[] racers = countdownController.Racers;
            if (racers == null) return;

            foreach (VehicleController vehicle in racers)
            {
                if (vehicle == null || warnedThisSequence.Contains(vehicle)) continue;

                Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float currentSpeed = GetRbVelocityMagnitude(rb);
                    if (currentSpeed > lockViolationThreshold)
                    {
                        string msg = $"[Phase6 validation] {vehicle.name} moved while controls should be " +
                                     $"locked (speed {currentSpeed:F3}) — auto-brake hold may not be sufficient " +
                                     $"on this track's grid slope, or SetControlsLocked isn't being respected.";
                        Debug.LogWarning(msg, vehicle);
                        warnedThisSequence.Add(vehicle);
                        validationWarnings.Add(msg);
                    }
                }
            }
        }

        private void Update()
        {
            if (showingGo)
            {
                goDisplayTimer -= Time.deltaTime;
                if (goDisplayTimer <= 0f)
                {
                    showingGo = false;
                    countdownDisplay = "";
                }
            }
        }

        private void OnGUI()
        {
            // Big Center Countdown / GO Display
            GUIStyle bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 72,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = showingGo ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.9f, 0.2f) }
            };

            if (!string.IsNullOrEmpty(countdownDisplay))
            {
                GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 100, 300, 150), countdownDisplay, bigStyle);
            }

            // Top Status Panel
            GUI.Box(new Rect(10, 10, 420, 130), "PHASE 6: RACE COUNTDOWN & CONTROLS LOCK");

            string stateColor = countdownController != null && countdownController.State == CountdownState.Racing ? "🟢" : "🟡";
            string stateText = countdownController != null
                ? $"{stateColor} State: {countdownController.State}   RaceTime: {countdownController.RaceTime:F2}s"
                : "No RaceCountdownController assigned";
            GUI.Label(new Rect(20, 35, 400, 24), stateText);

            string lockInfo = countdownController != null && countdownController.State != CountdownState.Racing
                ? "🔒 CONTROLS LOCKED (Auto-Brake Clamped)"
                : "🔓 CONTROLS UNLOCKED (Racing)";
            GUI.Label(new Rect(20, 60, 400, 20), lockInfo);

            string warningInfo = validationWarnings.Count == 0
                ? "✅ Physics Lock Check: ZERO drift (slope held OK)"
                : $"⚠️ Violations Detected: {validationWarnings.Count}";
            GUI.Label(new Rect(20, 80, 400, 20), warningInfo);

            if (GUI.Button(new Rect(20, 104, 160, 26), "🔄 Restart Countdown"))
            {
                countdownController?.BeginSequence();
            }

            GUI.Label(new Rect(190, 107, 230, 20), "Press W/S to test lock during 3-2-1");
        }

        private float GetRbVelocityMagnitude(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity.magnitude;
#else
            return rb.velocity.magnitude;
#endif
        }
    }
}
