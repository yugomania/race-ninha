using UnityEngine;

namespace ACR.Prototype
{
    /// <summary>
    /// PHASE 0.5 THROWAWAY PROTOTYPE CHASE CAMERA
    /// Purpose: Test third-person chase camera feel, speed FOV expansion, and impact shake.
    /// Serves Pillars: "Readable Chaos" & "One-Thumb Mastery".
    /// </summary>
    public class Prototype_CameraController : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 3.2f, -6.5f);
        public float followSpeed = 12f;
        public float baseFOV = 60f;
        public float maxFOV = 75f;

        private Camera _cam;
        private Rigidbody _targetRb;
        private float _shakeIntensity = 0f;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (target != null)
            {
                _targetRb = target.GetComponent<Rigidbody>();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position behind car aligned with car rotation
            Vector3 desiredPos = target.TransformPoint(offset);

            // Add shake offset if active
            if (_shakeIntensity > 0.01f)
            {
                desiredPos += Random.insideUnitSphere * _shakeIntensity;
                _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, Time.deltaTime * 6f);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

            // Look slightly ahead of car
            Vector3 lookTarget = target.position + target.forward * 4f + Vector3.up * 1.2f;
            transform.LookAt(lookTarget);

            // Speed-based dynamic FOV kick
            if (_cam != null && _targetRb != null)
            {
                float speedKmh = _targetRb.linearVelocity.magnitude * 3.6f;
                float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedKmh / 120f);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * 4f);
            }
        }

        public void TriggerShake(float intensity)
        {
            _shakeIntensity = intensity;
        }
    }
}
