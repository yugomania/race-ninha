using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// Disposable Phase 7 test scaffolding: shows live lap/checkpoint progress while racing,
    /// validates that checkpoints are crossed in sequence, then displays a WIN/LOSE overlay
    /// with finish time and restart once RaceManager reports a result.
    ///
    /// Turnkey setup: if references are not assigned in inspector, Start() will automatically
    /// construct a test track circuit, player vehicle, chase camera, countdown controller,
    /// lap system, and race manager.
    /// </summary>
    public class Phase7_TestHarness : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private LapSystem lapSystem;
        [SerializeField] private RaceCountdownController countdownController;
        [SerializeField] private VehicleController playerVehicle;
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private RaceManager raceManager;

        [Header("Vehicle Config")]
        public VehicleDataSO vehicleData;

        private RaceResult lastResult;

        private void OnEnable()
        {
            RaceManager.OnRaceCompleted += HandleRaceCompleted;
            RaceCountdownController.OnStagingComplete += HandleStagingComplete;
        }

        private void OnDisable()
        {
            RaceManager.OnRaceCompleted -= HandleRaceCompleted;
            RaceCountdownController.OnStagingComplete -= HandleStagingComplete;
        }

        private void Start()
        {
            if (lapSystem == null || countdownController == null || playerVehicle == null || trackManager == null)
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

            // Build Loop Circuit
            GameObject trackRoot = new GameObject("Track_Phase7_Circuit");

            CreateRoad(trackRoot.transform, "Road_MainStraight", new Vector3(0f, 0f, 30f), new Vector3(14f, 0.4f, 70f));
            CreateRoad(trackRoot.transform, "Road_Turn1", new Vector3(20f, 0f, 75f), new Vector3(35f, 0.4f, 20f));
            CreateRoad(trackRoot.transform, "Road_BackStraight", new Vector3(40f, 0f, 30f), new Vector3(14f, 0.4f, 70f));
            CreateRoad(trackRoot.transform, "Road_Turn2", new Vector3(20f, 0f, -15f), new Vector3(35f, 0.4f, 20f));

            // Checkpoints: 0=Finish line at Z=5, 1 at Z=55, 2 at Turn 1, 3 at Back straight, 4 at Turn 2
            GameObject cpRoot = new GameObject("Checkpoints");
            cpRoot.transform.SetParent(trackRoot.transform);

            TrackCheckpoint[] cps = new TrackCheckpoint[5];
            cps[0] = CreateCheckpoint(cpRoot.transform, 0, true, new Vector3(0f, 2.5f, 5f), Quaternion.identity, new Vector3(16f, 6f, 3f));
            cps[1] = CreateCheckpoint(cpRoot.transform, 1, false, new Vector3(0f, 2.5f, 55f), Quaternion.identity, new Vector3(16f, 6f, 3f));
            cps[2] = CreateCheckpoint(cpRoot.transform, 2, false, new Vector3(20f, 2.5f, 75f), Quaternion.Euler(0f, 90f, 0f), new Vector3(16f, 6f, 3f));
            cps[3] = CreateCheckpoint(cpRoot.transform, 3, false, new Vector3(40f, 2.5f, 30f), Quaternion.Euler(0f, 180f, 0f), new Vector3(16f, 6f, 3f));
            cps[4] = CreateCheckpoint(cpRoot.transform, 4, false, new Vector3(20f, 2.5f, -15f), Quaternion.Euler(0f, 270f, 0f), new Vector3(16f, 6f, 3f));

            // Start Grid (Slot 0 is pole, just behind Finish Line at Z = -2)
            GameObject gridRoot = new GameObject("StartGrid");
            gridRoot.transform.SetParent(trackRoot.transform);

            Transform[] gridTransforms = new Transform[4];
            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-2.8f, 0.5f, -2f),
                new Vector3(2.8f, 0.5f, -9f),
                new Vector3(-2.8f, 0.5f, -16f),
                new Vector3(2.8f, 0.5f, -23f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject slot = new GameObject($"GridSlot_{i}");
                slot.transform.SetParent(gridRoot.transform);
                slot.transform.position = gridOffsets[i];
                slot.transform.rotation = Quaternion.identity;
                gridTransforms[i] = slot.transform;
            }

            trackManager = trackRoot.AddComponent<TrackManager>();
            trackManager.SetCheckpoints(cps);
            trackManager.SetStartGridPositions(gridTransforms);

            // Build Player Vehicle
            GameObject car = new GameObject("Player_Vehicle_P1");
            car.transform.position = gridTransforms[0].position + Vector3.up * 0.8f;
            car.transform.rotation = gridTransforms[0].rotation;

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

            Rigidbody rb = car.AddComponent<Rigidbody>();
            playerVehicle = car.AddComponent<VehicleController>();

            KeyboardVehicleInput keyInput = car.AddComponent<KeyboardVehicleInput>();
            playerVehicle.SetInputSource(keyInput);

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
            if (fieldSockets != null) fieldSockets.SetValue(playerVehicle, sockets);

            var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldData != null) fieldData.SetValue(playerVehicle, vehicleData);

            // Camera
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

            // Setup Race Systems
            GameObject managersObj = new GameObject("RaceManagers");
            countdownController = managersObj.AddComponent<RaceCountdownController>();
            countdownController.Configure(trackManager, new VehicleController[] { playerVehicle }, chaseCam, 0);

            lapSystem = managersObj.AddComponent<LapSystem>();
            lapSystem.Configure(trackManager, countdownController);

            raceManager = managersObj.AddComponent<RaceManager>();
            raceManager.Configure(playerVehicle);
        }

        private GameObject CreateRoad(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent);
            road.transform.position = pos;
            road.transform.localScale = size;
            road.GetComponent<Renderer>().material.color = new Color(0.24f, 0.26f, 0.28f);
            return road;
        }

        private TrackCheckpoint CreateCheckpoint(Transform parent, int index, bool finishLine, Vector3 pos, Quaternion rot, Vector3 boxSize)
        {
            GameObject cpObj = new GameObject(finishLine ? $"Checkpoint_{index:D2}_FinishLine" : $"Checkpoint_{index:D2}");
            cpObj.transform.SetParent(parent);
            cpObj.transform.position = pos;
            cpObj.transform.rotation = rot;

            BoxCollider box = cpObj.AddComponent<BoxCollider>();
            box.size = boxSize;
            box.isTrigger = true;

            TrackCheckpoint cp = cpObj.AddComponent<TrackCheckpoint>();
            cp.checkpointIndex = index;
            cp.isFinishLine = finishLine;

            return cp;
        }

        private void HandleRaceCompleted(RaceResult result)
        {
            lastResult = result;
        }

        private void HandleStagingComplete()
        {
            lastResult = null;
        }

        private void OnGUI()
        {
            if (lastResult == null)
            {
                DrawProgressHUD();
            }
            else
            {
                DrawResultOverlay();
            }
        }

        private void DrawProgressHUD()
        {
            if (lapSystem == null || playerVehicle == null || trackManager == null) return;

            GUI.Box(new Rect(10, 10, 440, 140), "PHASE 7: LAP SYSTEM & RACE COMPLETION");

            string countdownState = countdownController != null ? countdownController.State.ToString() : "N/A";
            float raceTime = countdownController != null ? countdownController.RaceTime : 0f;
            GUI.Label(new Rect(20, 32, 400, 20), $"State: {countdownState}   |   Time: {raceTime:F2}s");

            if (lapSystem.TryGetProgress(playerVehicle, out LapProgress p))
            {
                GUIStyle lapStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow }
                };

                int displayLap = Mathf.Min(p.lapsCompleted + 1, trackManager.LapCount);
                string text = $"🏁 LAP {displayLap} / {trackManager.LapCount}   (Next Checkpoint: #{p.nextExpectedCheckpointIndex})";
                GUI.Label(new Rect(20, 56, 410, 30), text, lapStyle);

                GUI.Label(new Rect(20, 88, 410, 20), $"Anti-Cheat: Driving backward or cutting checkpoints is ignored.");
            }

            if (GUI.Button(new Rect(20, 112, 130, 26), "🔄 Restart Race"))
            {
                lastResult = null;
                countdownController?.BeginSequence();
            }

            if (GUI.Button(new Rect(160, 112, 160, 26), "⚡ Teleport to Checkpoint 1"))
            {
                TeleportNearCheckpoint(1);
            }

            if (GUI.Button(new Rect(330, 112, 110, 26), "⚡ Near Finish"))
            {
                TeleportNearCheckpoint(0);
            }
        }

        private void TeleportNearCheckpoint(int cpIndex)
        {
            if (trackManager == null || playerVehicle == null) return;
            TrackCheckpoint[] cps = trackManager.Checkpoints;
            if (cps != null && cpIndex >= 0 && cpIndex < cps.Length)
            {
                Transform cpT = cps[cpIndex].transform;
                playerVehicle.transform.position = cpT.position - cpT.forward * 8f + Vector3.up * 0.8f;
                playerVehicle.transform.rotation = cpT.rotation;
                Rigidbody rb = playerVehicle.GetComponent<Rigidbody>();
                if (rb != null)
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

        private void DrawResultOverlay()
        {
            // Dim background
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            GUIStyle bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = lastResult.won ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.25f, 0.25f) }
            };
            string resultText = lastResult.won ? "YOU WIN!" : $"FINISHED — P{lastResult.place}";
            GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height / 2f - 140, 500, 100), resultText, bigStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height / 2f - 30, 500, 40),
                      $"Finish time: {lastResult.finishTime:F2}s   (Place: P{lastResult.place})", subStyle);

            if (GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2f + 30, 160, 48), "Restart Race"))
            {
                lastResult = null;
                countdownController?.BeginSequence();
            }
        }
    }
}
