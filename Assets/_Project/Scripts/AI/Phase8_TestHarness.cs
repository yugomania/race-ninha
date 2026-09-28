using System.Collections.Generic;
using UnityEngine;
using ACR.Core;
using ACR.Race;
using ACR.Vehicle;

namespace ACR.AI
{
    /// <summary>
    /// PHASE 8 TEST HARNESS
    /// Turnkey validation for autonomous AI opponent vehicles racing alongside the player.
    /// Demonstrates:
    /// - Input Decoupling: AIController implements IVehicleInputSource with ZERO changes to VehicleController.
    /// - Multi-car grid staging, countdown lock, and simultaneous launch.
    /// - Autonomous checkpoint following, corner slowdown, and lap completion.
    /// - Meaningful race placement: player finishes 1st, 2nd, 3rd, or 4th against actual opponents.
    /// - Camera cycling: view the race from player or any AI opponent's perspective.
    /// </summary>
    public class Phase8_TestHarness : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private RaceCountdownController countdownController;
        [SerializeField] private LapSystem lapSystem;
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private ThirdPersonCameraController chaseCamera;

        [Header("Vehicle Config")]
        public VehicleDataSO vehicleData;

        private VehicleController playerVehicle;
        private readonly List<VehicleController> allVehicles = new List<VehicleController>();
        private readonly List<AIController> aiControllers = new List<AIController>();
        private RaceResult lastResult;
        private int activeCameraTargetIndex = 0;

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
            if (countdownController == null || trackManager == null)
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
                    vehicleData.topSpeed = 38f;
                    vehicleData.accelerationForce = 26f;
                    vehicleData.brakeForce = 45f;
                    vehicleData.coastingDrag = 4f;
                    vehicleData.maxSteerAngle = 32f;
                    vehicleData.steerSpeed = 6.5f;
                    vehicleData.suspensionRestDistance = 0.5f;
                    vehicleData.springStrength = 65f;
                    vehicleData.springDamper = 6.5f;
                    vehicleData.wheelRadius = 0.35f;
                    vehicleData.gripFactor = 0.88f;
                    vehicleData.mass = 1200f;
                }
            }

            // Build Wide Loop Circuit with smooth rounded corners suitable for multi-car racing
            GameObject trackRoot = new GameObject("Track_Phase8_Circuit");

            CreateRoad(trackRoot.transform, "Road_MainStraight", new Vector3(0f, 0f, 35f), new Vector3(18f, 0.4f, 80f));
            CreateRoad(trackRoot.transform, "Road_Turn1", new Vector3(25f, 0f, 85f), new Vector3(40f, 0.4f, 24f));
            CreateRoad(trackRoot.transform, "Road_BackStraight", new Vector3(50f, 0f, 35f), new Vector3(18f, 0.4f, 80f));
            CreateRoad(trackRoot.transform, "Road_Turn2", new Vector3(25f, 0f, -15f), new Vector3(40f, 0.4f, 24f));

            // Checkpoints: wide triggers spanning road
            GameObject cpRoot = new GameObject("Checkpoints");
            cpRoot.transform.SetParent(trackRoot.transform);

            TrackCheckpoint[] cps = new TrackCheckpoint[5];
            cps[0] = CreateCheckpoint(cpRoot.transform, 0, true, new Vector3(0f, 2.5f, 5f), Quaternion.identity, new Vector3(20f, 6f, 4f));
            cps[1] = CreateCheckpoint(cpRoot.transform, 1, false, new Vector3(0f, 2.5f, 65f), Quaternion.identity, new Vector3(20f, 6f, 4f));
            cps[2] = CreateCheckpoint(cpRoot.transform, 2, false, new Vector3(25f, 2.5f, 85f), Quaternion.Euler(0f, 90f, 0f), new Vector3(20f, 6f, 4f));
            cps[3] = CreateCheckpoint(cpRoot.transform, 3, false, new Vector3(50f, 2.5f, 35f), Quaternion.Euler(0f, 180f, 0f), new Vector3(20f, 6f, 4f));
            cps[4] = CreateCheckpoint(cpRoot.transform, 4, false, new Vector3(25f, 2.5f, -15f), Quaternion.Euler(0f, 270f, 0f), new Vector3(20f, 6f, 4f));

            // 4 Start Grid Slots (Staggered 2x2 F1 format)
            GameObject gridRoot = new GameObject("StartGrid");
            gridRoot.transform.SetParent(trackRoot.transform);

            Transform[] gridTransforms = new Transform[4];
            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-3.5f, 0.5f, -2f),   // Slot 0: Player Pole
                new Vector3(3.5f, 0.5f, -10f),   // Slot 1: AI 1
                new Vector3(-3.5f, 0.5f, -18f),  // Slot 2: AI 2
                new Vector3(3.5f, 0.5f, -26f)    // Slot 3: AI 3
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

            // Camera Setup
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

            // Build 4 Vehicles: 1 Player + 3 AI Opponents
            allVehicles.Clear();
            aiControllers.Clear();

            for (int i = 0; i < 4; i++)
            {
                bool isPlayer = (i == 0);
                string vehicleName = isPlayer ? "Player_Vehicle_P1" : $"AI_Opponent_{i}";
                Color vehicleColor = i switch
                {
                    0 => new Color(0.05f, 0.7f, 0.95f), // Cyan Player
                    1 => new Color(0.95f, 0.2f, 0.2f),  // Red AI 1
                    2 => new Color(0.95f, 0.75f, 0.1f), // Yellow AI 2
                    _ => new Color(0.2f, 0.85f, 0.3f)   // Green AI 3
                };

                GameObject car = new GameObject(vehicleName);
                car.transform.position = gridTransforms[i].position + Vector3.up * 0.8f;
                car.transform.rotation = gridTransforms[i].rotation;

                // Visual Body
                GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                chassis.transform.SetParent(car.transform);
                chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
                chassis.GetComponent<Renderer>().material.color = vehicleColor;
                Destroy(chassis.GetComponent<Collider>());

                BoxCollider col = car.AddComponent<BoxCollider>();
                col.size = new Vector3(1.8f, 0.8f, 4.0f);
                col.center = new Vector3(0f, 0.5f, 0f);

                Rigidbody rb = car.AddComponent<Rigidbody>();
                VehicleController vc = car.AddComponent<VehicleController>();

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
                if (fieldSockets != null) fieldSockets.SetValue(vc, sockets);

                var fieldData = typeof(VehicleController).GetField("vehicleData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldData != null) fieldData.SetValue(vc, vehicleData);

                // Assign Input Sources: Keyboard for player, AIController for opponents
                if (isPlayer)
                {
                    playerVehicle = vc;
                    KeyboardVehicleInput keyInput = car.AddComponent<KeyboardVehicleInput>();
                    vc.SetInputSource(keyInput);
                }
                else
                {
                    AIController ai = car.AddComponent<AIController>();
                    ai.Configure(trackManager);
                    vc.SetInputSource(ai);
                    aiControllers.Add(ai);
                }

                allVehicles.Add(vc);
            }

            // Setup Race Systems
            GameObject managersObj = new GameObject("RaceManagers");
            countdownController = managersObj.AddComponent<RaceCountdownController>();
            countdownController.Configure(trackManager, allVehicles.ToArray(), chaseCamera, 0);

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

        private void CycleCameraTarget()
        {
            if (allVehicles.Count == 0 || chaseCamera == null) return;
            activeCameraTargetIndex = (activeCameraTargetIndex + 1) % allVehicles.Count;
            chaseCamera.SetTarget(allVehicles[activeCameraTargetIndex].transform);
            chaseCamera.SnapToTarget();
        }

        private void OnGUI()
        {
            if (lastResult == null)
            {
                DrawRaceHUD();
            }
            else
            {
                DrawResultOverlay();
            }
        }

        private void DrawRaceHUD()
        {
            GUI.Box(new Rect(10, 10, 460, 240), "PHASE 8: AI OPPONENTS & MULTI-CAR RACING");

            string countdownState = countdownController != null ? countdownController.State.ToString() : "N/A";
            float raceTime = countdownController != null ? countdownController.RaceTime : 0f;
            GUI.Label(new Rect(20, 32, 420, 20), $"State: {countdownState}   |   Time: {raceTime:F2}s");

            // Live Standings Board
            GUI.Label(new Rect(20, 54, 420, 20), "Live Standings & Lap Progress:");

            int y = 74;
            for (int i = 0; i < allVehicles.Count; i++)
            {
                VehicleController v = allVehicles[i];
                if (v == null) continue;

                string role = (i == 0) ? "🎮 Player" : $"🤖 AI #{i}";
                string status = "Staging";

                if (lapSystem != null && lapSystem.TryGetProgress(v, out LapProgress p))
                {
                    if (p.finished)
                    {
                        status = "🏁 FINISHED";
                    }
                    else
                    {
                        int lap = Mathf.Min(p.lapsCompleted + 1, trackManager != null ? trackManager.LapCount : 3);
                        status = $"Lap {lap} (Next CP: #{p.nextExpectedCheckpointIndex})";
                    }
                }

                GUI.Label(new Rect(30, y, 400, 18), $"{role} ({v.name}): {status}");
                y += 20;
            }

            // Controls
            if (GUI.Button(new Rect(20, 168, 140, 28), "🔄 Restart Race"))
            {
                lastResult = null;
                countdownController?.BeginSequence();
            }

            string currentCamName = (allVehicles.Count > activeCameraTargetIndex && allVehicles[activeCameraTargetIndex] != null)
                ? allVehicles[activeCameraTargetIndex].name
                : "Unknown";

            if (GUI.Button(new Rect(170, 168, 180, 28), $"🎥 Cycle Cam ({currentCamName})"))
            {
                CycleCameraTarget();
            }

            GUI.Label(new Rect(20, 208, 430, 20), "Select AI vehicles in Scene view to observe cyan target ray.");
        }

        private void DrawResultOverlay()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            GUIStyle bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = lastResult.won ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f) }
            };

            string resultTitle = lastResult.won ? "🏆 1ST PLACE — YOU WIN!" : $"FINISHED — PLACE {lastResult.place} OF 4";
            GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height / 2f - 140, 600, 100), resultTitle, bigStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height / 2f - 30, 500, 40),
                      $"Finish Time: {lastResult.finishTime:F2}s", subStyle);

            if (GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2f + 30, 160, 48), "Restart Race"))
            {
                lastResult = null;
                countdownController?.BeginSequence();
            }
        }
    }
}
