using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(Camera))]
    public class RosvikTopDownCamera : MonoBehaviour {
        public Transform target;
        public float height = 45f;
        public float followSharpness = 10f;
        public float orthographicSize = 24f;
        public float minSize = 10f;
        public float maxSize = 38f;
        public float zoomStep = 1.5f;

        Camera cam;
        Vector3 smoothed;
        bool initialized;

        void Awake() {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        void LateUpdate() {
            if (!target) return;
            if (!cam) cam = GetComponent<Camera>();

            if (Mouse.current != null) {
                float scroll = Mouse.current.scroll.ReadValue().y / 120f;
                if (Mathf.Abs(scroll) > .01f)
                    orthographicSize = Mathf.Clamp(orthographicSize - scroll * zoomStep, minSize, maxSize);
            }

            Vector3 wanted = new Vector3(target.position.x, height, target.position.z);
            if (!initialized) { smoothed = wanted; initialized = true; }
            else smoothed = Vector3.Lerp(smoothed, wanted, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            transform.SetPositionAndRotation(smoothed, Quaternion.Euler(90f, 0f, 0f));
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthographicSize, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
