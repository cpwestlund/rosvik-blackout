using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(CharacterController))]
    public class RosvikPlayerController : MonoBehaviour {
        [Header("Movement")]
        public float walkSpeed = 3.4f;
        public float sprintSpeed = 5.6f;
        public float acceleration = 18f;
        public float deceleration = 24f;
        public float turnSharpness = 14f;
        public float gravity = -24f;

        CharacterController controller;
        Camera viewCamera;
        Vector3 planarVelocity;
        float verticalVelocity;

        void Awake() {
            controller = GetComponent<CharacterController>();
            viewCamera = Camera.main;
        }

        void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!viewCamera) viewCamera = Camera.main;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

            Vector3 input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f) input.Normalize();

            // Derive the movement basis from actual screen-space rays projected onto the
            // player's ground plane. This guarantees that W is visually straight up on the
            // screen and D is visually straight right, even with an isometric yaw/pitch.
            Vector3 screenUp = Vector3.forward;
            Vector3 screenRight = Vector3.right;
            if (!TryGetScreenGroundBasis(out screenUp, out screenRight) && viewCamera) {
                screenUp = viewCamera.transform.forward;
                screenRight = viewCamera.transform.right;
                screenUp.y = 0f;
                screenRight.y = 0f;
                if (screenUp.sqrMagnitude > .001f) screenUp.Normalize();
                if (screenRight.sqrMagnitude > .001f) screenRight.Normalize();
            }

            Vector3 desiredDirection = screenUp * input.z + screenRight * input.x;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

            bool sprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float targetSpeed = sprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = desiredDirection * targetSpeed;
            float rate = desiredDirection.sqrMagnitude > .001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            CollisionFlags flags = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (desiredDirection.sqrMagnitude > .01f) {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }
        }

        bool TryGetScreenGroundBasis(out Vector3 screenUp, out Vector3 screenRight) {
            screenUp = Vector3.forward;
            screenRight = Vector3.right;
            if (!viewCamera) return false;

            Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!GroundHit(viewCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f)), ground, out Vector3 center)) return false;
            if (!GroundHit(viewCamera.ViewportPointToRay(new Vector3(.5f, .62f, 0f)), ground, out Vector3 up)) return false;
            if (!GroundHit(viewCamera.ViewportPointToRay(new Vector3(.62f, .5f, 0f)), ground, out Vector3 right)) return false;

            screenUp = up - center;
            screenRight = right - center;
            screenUp.y = 0f;
            screenRight.y = 0f;
            if (screenUp.sqrMagnitude < .0001f || screenRight.sqrMagnitude < .0001f) return false;
            screenUp.Normalize();
            screenRight.Normalize();

            // Remove tiny numerical skew so diagonals retain equal speed and the four
            // cardinal keys remain visually perpendicular on screen.
            screenRight = Vector3.Cross(Vector3.up, screenUp).normalized;
            Vector3 projectedRight = right - center;
            projectedRight.y = 0f;
            if (Vector3.Dot(screenRight, projectedRight) < 0f) screenRight = -screenRight;
            return true;
        }

        static bool GroundHit(Ray ray, Plane ground, out Vector3 point) {
            point = Vector3.zero;
            if (!ground.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }
    }
}
