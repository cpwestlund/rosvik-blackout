using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class IsometricCameraRig : MonoBehaviour {
        public Transform target;

        [Header("Framing")]
        public float yaw = 45f;
        public float pitch = 46f;
        public Vector3 focusOffset = new Vector3(0f, 1.2f, 0f);
        public float followSharpness = 9f;

        [Header("Orthographic Zoom")]
        public float orthographicSize = 8.15f;
        public float defaultSize = 8.15f;
        public float minSize = 5.2f;
        public float maxSize = 19.5f;
        public float zoomStep = 0.8f;
        public float keyboardZoomSpeed = 5.5f;
        public float zoomSharpness = 11f;

        Camera cam;
        Vector3 smoothedFocus;
        bool initialized;

        void Awake() {
            cam = GetComponent<Camera>();
            if (cam) cam.orthographic = true;
            orthographicSize = Mathf.Clamp(orthographicSize, minSize, maxSize);
            defaultSize = Mathf.Clamp(defaultSize, minSize, maxSize);
        }

        void LateUpdate() {
            if (!target) return;
            if (!cam) cam = GetComponent<Camera>();

            ReadZoomInput();

            if (cam)
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthographicSize,
                    1f - Mathf.Exp(-zoomSharpness * Time.deltaTime));

            Vector3 wantedFocus = target.position + focusOffset;
            if (!initialized) {
                smoothedFocus = wantedFocus;
                initialized = true;
            } else {
                smoothedFocus = Vector3.Lerp(smoothedFocus, wantedFocus,
                    1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            // Camera angle stays fixed; movement code projects the real screen axes onto
            // the ground, so the controls remain stable regardless of this yaw/pitch.
            Vector3 offset = rotation * new Vector3(0f, 0f, -22f);
            transform.SetPositionAndRotation(smoothedFocus + offset, rotation);
        }

        void ReadZoomInput() {
            if (Mouse.current != null) {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > .01f) {
                    // Input System reports very different wheel magnitudes across mice and
                    // touchpads. Direction-based stepping is predictable on all of them.
                    ChangeZoom(-Mathf.Sign(scrollY) * zoomStep);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool zoomIn = keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed || keyboard.pageUpKey.isPressed;
            bool zoomOut = keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed || keyboard.pageDownKey.isPressed;
            float delta = keyboardZoomSpeed * Time.deltaTime;
            if (zoomIn && !zoomOut) ChangeZoom(-delta);
            else if (zoomOut && !zoomIn) ChangeZoom(delta);

            if (keyboard.homeKey.wasPressedThisFrame)
                orthographicSize = Mathf.Clamp(defaultSize, minSize, maxSize);
        }

        public void ChangeZoom(float delta) {
            orthographicSize = Mathf.Clamp(orthographicSize + delta, minSize, maxSize);
        }

        public void SetZoom(float size) {
            orthographicSize = Mathf.Clamp(size, minSize, maxSize);
        }
    }
}
