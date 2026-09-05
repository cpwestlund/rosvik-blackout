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
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!viewCamera) viewCamera = Camera.main;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

            Vector3 input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f) input.Normalize();

            // Movement is camera-relative. W always means "up/away" on screen,
            // A/D always feel left/right regardless of camera yaw.
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (viewCamera) {
                forward = viewCamera.transform.forward;
                right = viewCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                if (forward.sqrMagnitude > .001f) forward.Normalize();
                if (right.sqrMagnitude > .001f) right.Normalize();
            }

            Vector3 desiredDirection = forward * input.z + right * input.x;
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
    }
}
