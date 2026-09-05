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
        public float orthographicSize = 10.5f;
        public float minSize = 7.5f;
        public float maxSize = 15f;
        public float zoomStep = 0.9f;

        Camera cam;
        Vector3 smoothedFocus;
        bool initialized;

        void Awake() {
            cam = GetComponent<Camera>();
            if (cam) cam.orthographic = true;
        }

        void LateUpdate() {
            if (!target) return;
            if (!cam) cam = GetComponent<Camera>();

            if (Mouse.current != null) {
                float scroll = Mouse.current.scroll.ReadValue().y / 120f;
                if (Mathf.Abs(scroll) > .01f)
                    orthographicSize = Mathf.Clamp(orthographicSize - scroll * zoomStep, minSize, maxSize);
            }

            if (cam)
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthographicSize, 1f - Mathf.Exp(-10f * Time.deltaTime));

            Vector3 wantedFocus = target.position + focusOffset;
            if (!initialized) {
                smoothedFocus = wantedFocus;
                initialized = true;
            } else {
                smoothedFocus = Vector3.Lerp(smoothedFocus, wantedFocus, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            // Camera angle is intentionally fixed. This keeps WASD readable and prevents
            // the disorienting rotating-controls problem from the prototype.
            Vector3 offset = rotation * new Vector3(0f, 0f, -22f);
            transform.SetPositionAndRotation(smoothedFocus + offset, rotation);
        }
    }
}
