using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class IsometricCameraRig : MonoBehaviour {
        public Transform target;
        public float yaw = 45f, pitch = 48f, distance = 15f;
        public float orbitSensitivity = .18f, zoomSensitivity = 1.4f;
        public float minDistance = 8f, maxDistance = 23f;
        public Vector3 focusOffset = new(0, 1.1f, 0);

        void LateUpdate() {
            if (!target) return;

            var mouse = Mouse.current;
            if (mouse != null) {
                if (mouse.middleButton.isPressed) {
                    Vector2 delta = mouse.delta.ReadValue();
                    // Grab-style orbit: drag right = world follows right, not inverted.
                    yaw -= delta.x * orbitSensitivity;
                    pitch += delta.y * orbitSensitivity;
                    pitch = Mathf.Clamp(pitch, 32f, 62f);
                }
                float scroll = mouse.scroll.ReadValue().y / 120f;
                if (Mathf.Abs(scroll) > .01f)
                    distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 focus = target.position + focusOffset;
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
        }
    }
}