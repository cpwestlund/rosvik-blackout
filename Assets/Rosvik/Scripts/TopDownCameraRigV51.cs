using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(Camera))]
    public class TopDownCameraRigV51 : MonoBehaviour {
        public Transform target;
        [Range(60f, 89f)] public float pitch = 80f;
        public float yaw = 0f;
        public float distance = 28f;
        public float orthographicSize = 11.5f;
        public float minSize = 7.5f;
        public float maxSize = 18f;
        public float followSharpness = 12f;
        public float zoomStep = .8f;
        public Vector3 focusOffset = new Vector3(0f, .7f, 0f);

        Camera cam;
        Vector3 smoothedFocus;
        bool initialized;

        void Awake() {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 500f;
        }

        void LateUpdate() {
            if (!target) return;
            if (!cam) cam = GetComponent<Camera>();

            if (Mouse.current != null) {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f)
                    orthographicSize = Mathf.Clamp(orthographicSize - Mathf.Sign(scroll) * zoomStep, minSize, maxSize);
            }

            Vector3 wanted = target.position + focusOffset;
            if (!initialized) {
                smoothedFocus = wanted;
                initialized = true;
            } else {
                float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
                smoothedFocus = Vector3.Lerp(smoothedFocus, wanted, t);
            }

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.SetPositionAndRotation(smoothedFocus + offset, rotation);
        }
    }
}
