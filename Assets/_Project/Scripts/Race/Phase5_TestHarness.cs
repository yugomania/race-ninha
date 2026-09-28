using System.Collections.Generic;
using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Race
{
    /// <summary>
    /// PHASE 5 TEST HARNESS
    /// Attach to an empty GameObject in any test scene.
    /// Provides turnkey verification for:
    /// - Track structure and starting grid (TrackManager)
    /// - Ordered Checkpoint triggers (TrackCheckpoint)
    /// - Authoring validation catches errors (Validate Track context menu / button)
    /// - Starting grid position & rotation queries (pole position to grid slot 3)
    /// - Static OnVehiclePassedCheckpoint event subscription with live event log
    /// </summary>
    public class Phase5_TestHarness : MonoBehaviour
    {
        [Header("Data Asset")]
        public VehicleDataSO vehicleData;

        private TrackManager _trackManager;
        private List<TrackCheckpoint> _checkpoints = new List<TrackCheckpoint>();
        private List<Transform> _gridTransforms = new List<Transform>();
        private VehicleController _vehicle;
        private Rigidbody _vehicleRb;
        private ThirdPersonCameraController _chaseCamera;

        private string _lastEventMessage = "None yet (drive through checkpoints)";
        private readonly List<string> _eventHistory = new List<string>();
        private string _validationStatus = "Not Run";
        private bool _validationPassed = false;

        private void OnEnable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint += HandleCheckpointPassed;
        }

        private void OnDisable()
        {
            TrackCheckpoint.OnVehiclePassedCheckpoint -= HandleCheckpointPassed;
        }

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

            BuildCircuitTrack();
            BuildVehicle();
            SetupCamera();

            // Run initial validation
            RunTrackValidation();
        }

        private void BuildCircuitTrack()
        {
            GameObject trackRoot = new GameObject("Track_PrototypeLoop_01");

            // Road segments forming a rounded rectangular racing loop
            // Segment 1: Main Straight (Z: 0 to 60)
            CreateRoadSegment(trackRoot.transform, "Road_MainStraight", new Vector3(0f, 0f, 30f), new Vector3(14f, 0.4f, 70f), new Color(0.22f, 0.24f, 0.26f));

            // Segment 2: North Turn 1 (Curved right)
            CreateRoadSegment(trackRoot.transform, "Road_Turn1", new Vector3(20f, 0f, 75f), new Vector3(35f, 0.4f, 20f), new Color(0.25f, 0.27f, 0.3f));

            // Segment 3: Back Straight with elevation crest (X: 40, Z: 65 to -5)
            GameObject backStraight = CreateRoadSegment(trackRoot.transform, "Road_BackStraight", new Vector3(40f, 1.5f, 30f), new Vector3(14f, 0.4f, 70f), new Color(0.2f, 0.22f, 0.25f));
            backStraight.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Segment 4: South Turn 2 (Connecting back to main straight)
            CreateRoadSegment(trackRoot.transform, "Road_Turn2", new Vector3(20f, 0f, -15f), new Vector3(35f, 0.4f, 20f), new Color(0.25f, 0.27f, 0.3f));

            // Inner and Outer Curbs (Rumble strips)
            CreateCurb(trackRoot.transform, new Vector3(-7.5f, 0.2f, 30f), new Vector3(1.2f, 0.3f, 70f));
            CreateCurb(trackRoot.transform, new Vector3(7.5f, 0.2f, 30f), new Vector3(1.2f, 0.3f, 70f));

            // --- Checkpoints ---
            GameObject cpRoot = new GameObject("Checkpoints");
            cpRoot.transform.SetParent(trackRoot.transform);

            // Checkpoint 0 (Finish Line) — on main straight at Z = 5
            _checkpoints.Add(CreateCheckpoint(cpRoot.transform, 0, true, new Vector3(0f, 2.5f, 5f), Quaternion.identity, new Vector3(16f, 5f, 3f)));

            // Checkpoint 1 — end of main straight at Z = 55
            _checkpoints.Add(CreateCheckpoint(cpRoot.transform, 1, false, new Vector3(0f, 2.5f, 55f), Quaternion.identity, new Vector3(16f, 5f, 3f)));

            // Checkpoint 2 — mid Turn 1 at X = 20, Z = 75
            _checkpoints.Add(CreateCheckpoint(cpRoot.transform, 2, false, new Vector3(20f, 2.5f, 75f), Quaternion.Euler(0f, 90f, 0f), new Vector3(16f, 5f, 3f)));

            // Checkpoint 3 — mid Back Straight at X = 40, Z = 30
            _checkpoints.Add(CreateCheckpoint(cpRoot.transform, 3, false, new Vector3(40f, 3.5f, 30f), Quaternion.Euler(0f, 180f, 0f), new Vector3(16f, 5f, 3f)));

            // Checkpoint 4 — mid Turn 2 at X = 20, Z = -15
            _checkpoints.Add(CreateCheckpoint(cpRoot.transform, 4, false, new Vector3(20f, 2.5f, -15f), Quaternion.Euler(0f, 270f, 0f), new Vector3(16f, 5f, 3f)));

            // --- Starting Grid (4 slots, staggered 2x2 F1 style behind finish line) ---
            GameObject gridRoot = new GameObject("StartGrid");
            gridRoot.transform.SetParent(trackRoot.transform);

            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-2.8f, 0.5f, -2f),   // Slot 0: Pole position (front left)
                new Vector3(2.8f, 0.5f, -9f),    // Slot 1: Front right
                new Vector3(-2.8f, 0.5f, -16f),  // Slot 2: Row 2 left
                new Vector3(2.8f, 0.5f, -23f)    // Slot 3: Row 2 right
            };

            for (int i = 0; i < gridOffsets.Length; i++)
            {
                GameObject slot = new GameObject($"GridSlot_{i}");
                slot.transform.SetParent(gridRoot.transform);
                slot.transform.position = gridOffsets[i];
                slot.transform.rotation = Quaternion.identity; // Facing forward (Z+)

                // Visual grid marker on ground
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Marker_{i}";
                marker.transform.SetParent(slot.transform);
                marker.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                marker.transform.localScale = new Vector3(2.5f, 0.05f, 4.0f);
                marker.GetComponent<Renderer>().material.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
                Destroy(marker.GetComponent<Collider>());

                _gridTransforms.Add(slot.transform);
            }

            // --- TrackManager Component ---
            _trackManager = trackRoot.AddComponent<TrackManager>();
            _trackManager.SetCheckpoints(_checkpoints.ToArray());
            _trackManager.SetStartGridPositions(_gridTransforms.ToArray());
        }

        private GameObject CreateRoadSegment(Transform parent, string name, Vector3 pos, Vector3 size, Color col)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent);
            road.transform.position = pos;
            road.transform.localScale = size;
            road.GetComponent<Renderer>().material.color = col;
            return road;
        }

        private void CreateCurb(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = "Track_Curb";
            curb.transform.SetParent(parent);
            curb.transform.position = pos;
            curb.transform.localScale = size;
            curb.GetComponent<Renderer>().material.color = new Color(0.85f, 0.15f, 0.15f);
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

        private void BuildVehicle()
        {
            GameObject car = new GameObject("Player_Vehicle_Phase5");
            Vector3 startPos = _trackManager.GetStartPosition(0);
            Quaternion startRot = _trackManager.GetStartRotation(0);
            car.transform.position = startPos + Vector3.up * 1.0f;
            car.transform.rotation = startRot;

            // Chassis Mesh
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

            _vehicleRb = car.AddComponent<Rigidbody>();
            _vehicle = car.AddComponent<VehicleController>();

            KeyboardVehicleInput keyInput = car.AddComponent<KeyboardVehicleInput>();
            _vehicle.SetInputSource(keyInput);

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

        private void SetupCamera()
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

        private void HandleCheckpointPassed(TrackCheckpoint checkpoint, VehicleController vehicle)
        {
            if (vehicle != _vehicle) return;

            string typeStr = checkpoint.isFinishLine ? "🏁 FINISH LINE" : "Checkpoint";
            _lastEventMessage = $"{typeStr} #{checkpoint.checkpointIndex} crossed!";

            string logEntry = $"[{Time.time:F1}s] {typeStr} #{checkpoint.checkpointIndex}";
            _eventHistory.Insert(0, logEntry);
            if (_eventHistory.Count > 5) _eventHistory.RemoveAt(_eventHistory.Count - 1);

            Debug.Log($"[Phase5] Event received: {vehicle.name} passed Checkpoint {checkpoint.checkpointIndex} (isFinishLine: {checkpoint.isFinishLine})");
        }

        public void RunTrackValidation()
        {
            if (_trackManager != null)
            {
                _validationPassed = _trackManager.Validate();
                _validationStatus = _validationPassed ? "✅ VALIDATED OK" : "❌ VALIDATION FAILED";
            }
        }

        private void TeleportToGridSlot(int slot)
        {
            if (_vehicle == null || _trackManager == null) return;

            Vector3 pos = _trackManager.GetStartPosition(slot);
            Quaternion rot = _trackManager.GetStartRotation(slot);

            _vehicle.transform.position = pos + Vector3.up * 0.8f;
            _vehicle.transform.rotation = rot;

            ResetRbVelocity(_vehicleRb);
            if (_chaseCamera != null) _chaseCamera.SnapToTarget();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 380, 320), "PHASE 5: TRACK MANAGER & CHECKPOINTS");

            GUI.Label(new Rect(25, 40, 360, 20), $"Track Validation: {_validationStatus}");
            GUI.Label(new Rect(25, 60, 360, 20), $"Structure: {_trackManager.CheckpointCount} Checkpoints | {_trackManager.StartGridPositions.Length} Grid Slots | {_trackManager.LapCount} Laps");
            GUI.Label(new Rect(25, 80, 360, 20), $"Last Crossed: {_lastEventMessage}");

            // Grid Spawn Buttons
            GUI.Label(new Rect(25, 105, 120, 20), "Spawn At Grid:");
            for (int i = 0; i < 4; i++)
            {
                if (GUI.Button(new Rect(140 + (i * 55), 104, 50, 24), $"Slot {i}"))
                {
                    TeleportToGridSlot(i);
                }
            }

            // Validation Controls
            if (GUI.Button(new Rect(25, 136, 165, 26), "🔍 Validate Track"))
            {
                RunTrackValidation();
            }

            if (GUI.Button(new Rect(195, 136, 185, 26), "⚠️ Test Missing Finish Line"))
            {
                // Break finish line flag to demonstrate validation catches errors
                if (_checkpoints.Count > 0)
                {
                    _checkpoints[0].isFinishLine = false;
                    RunTrackValidation();
                }
            }

            if (!_validationPassed)
            {
                if (GUI.Button(new Rect(25, 166, 165, 24), "🔧 Restore Finish Line"))
                {
                    if (_checkpoints.Count > 0)
                    {
                        _checkpoints[0].isFinishLine = true;
                        RunTrackValidation();
                    }
                }
            }

            // Recent Event Log
            GUI.Label(new Rect(25, 196, 350, 20), "Live Event Stream (OnVehiclePassedCheckpoint):");
            int y = 218;
            foreach (var log in _eventHistory)
            {
                GUI.Label(new Rect(30, y, 340, 18), $"• {log}");
                y += 18;
            }

            GUI.Label(new Rect(25, 292, 350, 20), "WASD = Drive | Space = Brake | C = Shake/Kick");
        }

        private void ResetRbVelocity(Rigidbody rb)
        {
            if (rb == null) return;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
    }
}
