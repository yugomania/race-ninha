using System.Collections.Generic;
using UnityEngine;

namespace ACR.Prototype
{
    /// <summary>
    /// PHASE 0.5 THROWAWAY PROTOTYPE SCENE GENERATOR
    /// Procedurally builds the gray-box test environment:
    /// - Flat test loop with chicane
    /// - Jump ramp with gap landing
    /// - Target dummy karts
    /// - Firing homing missiles on Spacebar / Tap
    /// </summary>
    public class Prototype_GameManager : MonoBehaviour
    {
        [Header("Prefabs & References")]
        public GameObject vehiclePrefab;
        public GameObject homingMissilePrefab;
        public Camera mainCamera;

        [Header("State")]
        public Prototype_ArcadeVehicle playerVehicle;
        public List<Transform> dummyTargets = new List<Transform>();

        private void Start()
        {
            BuildGrayBoxEnvironment();
            SpawnPlayer();
            SpawnDummyTargets();
        }

        private void Update()
        {
            // Firing prototype homing weapon
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                FirePrototypeMissile();
            }

            // Quick reset
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPlayer();
            }
        }

        private void BuildGrayBoxEnvironment()
        {
            // 1. Main Ground Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Graybox_Ground";
            floor.transform.localScale = new Vector3(50f, 1f, 50f);
            floor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.22f, 0.25f);

            // 2. Jump Ramp
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Graybox_Ramp";
            ramp.transform.position = new Vector3(0f, 1.5f, 40f);
            ramp.transform.localScale = new Vector3(8f, 0.5f, 12f);
            ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
            ramp.GetComponent<Renderer>().material.color = new Color(0.85f, 0.55f, 0.15f);

            // 3. Chicane Obstacle Blocks
            for (int i = 0; i < 4; i++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Graybox_Chicane_{i}";
                float x = (i % 2 == 0) ? -5f : 5f;
                block.transform.position = new Vector3(x, 1f, 80f + i * 18f);
                block.transform.localScale = new Vector3(6f, 2f, 2f);
                block.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f);
            }
        }

        private void SpawnPlayer()
        {
            GameObject carObj = new GameObject("Player_Prototype_Vehicle");
            carObj.transform.position = new Vector3(0f, 1.2f, 0f);

            // Capsule chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chassis.transform.SetParent(carObj.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 2.2f, 0.8f);
            chassis.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 0.9f);
            Destroy(chassis.GetComponent<Collider>()); // Rigidbody uses parent box collider

            BoxCollider col = carObj.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.8f, 4.0f);
            col.center = new Vector3(0f, 0.5f, 0f);

            playerVehicle = carObj.AddComponent<Prototype_ArcadeVehicle>();

            // Setup camera
            if (mainCamera != null)
            {
                var camCtrl = mainCamera.gameObject.AddComponent<Prototype_CameraController>();
                camCtrl.target = carObj.transform;
            }
        }

        private void SpawnDummyTargets()
        {
            float[] zPositions = new float[] { 25f, 70f, 120f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummy.name = $"Dummy_Kart_{i + 1}";
                dummy.transform.position = new Vector3((i % 2 == 0 ? 3f : -3f), 0.75f, zPositions[i]);
                dummy.transform.localScale = new Vector3(1.8f, 0.8f, 3.5f);
                dummy.GetComponent<Renderer>().material.color = new Color(0.9f, 0.8f, 0.2f);

                Rigidbody rb = dummy.AddComponent<Rigidbody>();
                rb.mass = 600f;

                dummyTargets.Add(dummy.transform);
            }
        }

        private void FirePrototypeMissile()
        {
            if (playerVehicle == null) return;

            // Find closest dummy in front of player
            Transform bestTarget = null;
            float closestDist = float.MaxValue;

            foreach (var dummy in dummyTargets)
            {
                if (dummy == null) continue;
                Vector3 toDummy = dummy.position - playerVehicle.transform.position;
                if (Vector3.Dot(playerVehicle.transform.forward, toDummy.normalized) > 0.3f)
                {
                    float dist = toDummy.magnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = dummy;
                    }
                }
            }

            GameObject missileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            missileObj.name = "Prototype_Missile";
            missileObj.transform.position = playerVehicle.transform.position + playerVehicle.transform.forward * 3f + Vector3.up * 0.8f;
            missileObj.transform.localScale = Vector3.one * 0.6f;
            missileObj.GetComponent<Renderer>().material.color = new Color(1f, 0.2f, 0.2f);

            var missile = missileObj.AddComponent<Prototype_HomingMissile>();
            missile.Initialize(bestTarget);
        }

        private void ResetPlayer()
        {
            if (playerVehicle != null)
            {
                playerVehicle.transform.position = new Vector3(0f, 1.2f, 0f);
                playerVehicle.transform.rotation = Quaternion.identity;
                var rb = playerVehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 280, 110), "PHASE 0.5 GRAY-BOX PROTOTYPE");
            GUI.Label(new Rect(20, 35, 260, 20), $"Speed: {(playerVehicle != null ? Mathf.Round(playerVehicle.CurrentSpeedKmh) : 0)} KM/H");
            GUI.Label(new Rect(20, 55, 260, 20), "Controls: W/S or Arrows to Drive");
            GUI.Label(new Rect(20, 75, 260, 20), "SPACE: Fire Homing Missile");
            GUI.Label(new Rect(20, 95, 260, 20), "SHIFT: Drift | R: Reset");
        }
    }
}
