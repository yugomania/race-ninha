using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// Touch input source for mobile. Deliberately minimal for Phase 3: raw screen-zone touch
    /// handling (left half = steer via horizontal drag, right half = throttle on touch) rather
    /// than styled UI buttons — the polished HUD buttons are a Phase 15 (Race UI) concern and
    /// will call into the same public fields this class exposes, so gameplay code never changes.
    ///
    /// Implements the two touch-assist options called for in the system prompt:
    ///   - autoAccelerate: player doesn't need to hold a throttle input at all, just steers.
    ///   - steerAssist: gently recenters steering when the player isn't actively dragging,
    ///     so small thumb jitter doesn't translate into constant fishtailing.
    /// Both are public toggles so a future Settings screen (Phase 25) can bind to them directly.
    /// 
    /// Serves Pillar: "One-Thumb Mastery".
    /// </summary>
    public class TouchVehicleInput : MonoBehaviour, IVehicleInputSource
    {
        [Header("Assist Options (togglable from Settings later)")]
        public bool autoAccelerate = true;
        public bool steerAssist = true;
        [Tooltip("How quickly steering recenters when steerAssist is on and no drag is active.")]
        [SerializeField] private float steerAssistRecenterSpeed = 3f;

        [Header("Tuning")]
        [Tooltip("Screen-width fraction a steering drag needs to reach max steer input.")]
        [SerializeField] private float steerDragRangePixels = 200f;

        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Brake { get; private set; }
        public bool NitroRequested { get; private set; }

        private int steerTouchId = -1;
        private Vector2 steerTouchStartPos;
        private bool throttleTouchActive;

        /// <summary>
        /// Called externally by a nitro button's press/release handler (a temporary OnGUI button for
        /// now — Phase 15's real HUD button will call this exact method on pointer down/up, so this
        /// won't need to change when the polished UI arrives). Deliberately not screen-zone-parsed
        /// like steer/throttle, since nitro is a discrete button press, not a drag gesture.
        /// </summary>
        public void SetNitroButtonHeld(bool held)
        {
            NitroRequested = held;
        }

        private void Update()
        {
            bool throttleHeld = false;
            float rawSteer = 0f;
            bool steerTouchFoundThisFrame = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool isLeftHalf = touch.position.x < Screen.width * 0.5f;

                if (isLeftHalf)
                {
                    // Steering zone: drag left/right from where the touch began.
                    if (touch.phase == TouchPhase.Began)
                    {
                        steerTouchId = touch.fingerId;
                        steerTouchStartPos = touch.position;
                    }

                    if (touch.fingerId == steerTouchId && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                    {
                        float deltaX = touch.position.x - steerTouchStartPos.x;
                        rawSteer = Mathf.Clamp(deltaX / steerDragRangePixels, -1f, 1f);
                        steerTouchFoundThisFrame = true;
                    }
                }
                else
                {
                    // Throttle zone: any touch on the right half counts as "accelerate."
                    if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                    {
                        throttleHeld = true;
                    }
                }
            }

            if (!steerTouchFoundThisFrame)
            {
                steerTouchId = -1;
            }

            // Apply steer assist: recenter smoothly if no active steering drag.
            if (steerTouchFoundThisFrame)
            {
                Steer = rawSteer;
            }
            else if (steerAssist)
            {
                Steer = Mathf.MoveTowards(Steer, 0f, steerAssistRecenterSpeed * Time.deltaTime);
            }
            else
            {
                Steer = 0f;
            }

            throttleTouchActive = throttleHeld;

            // Apply auto-accelerate: throttle is always "on" unless the player is braking.
            Throttle = autoAccelerate ? 1f : (throttleTouchActive ? 1f : 0f);

            // Brake is a separate concern (dedicated button in Phase 15 UI) — placeholder false for now.
            Brake = false;
        }
    }
}
