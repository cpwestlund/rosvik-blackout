using UnityEngine;

namespace Rosvik.Blackout {
    public class IsometricCameraRig : MonoBehaviour {
        public Transform target;
        public float yaw = 45f, pitch = 48f, distance = 15f;
        public float orbitSensitivity = .22f, zoomSensitivity = 1.4f;
        public float minDistance = 8f, maxDistance = 23f;
        public Vector3 focusOffset = new(0, 1.1f, 0);

        void LateUpdate() {
            if (!target) return;
            if (Input.GetMouseButton(2)) {
                yaw += Input.GetAxis("Mouse X") * orbitSensitivity * 12f; // non-inverted
                pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity * 9f;
                pitch = Mathf.Clamp(pitch, 32f, 62f);
            }
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * zoomSensitivity, minDistance, maxDistance);
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 focus = target.position + focusOffset;
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
        }
    }
}