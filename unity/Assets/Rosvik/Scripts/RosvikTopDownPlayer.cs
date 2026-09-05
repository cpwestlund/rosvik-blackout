using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(CharacterController))]
    public class RosvikTopDownPlayer : MonoBehaviour {
        public float walkSpeed = 4.0f;
        public float sprintSpeed = 6.4f;
        public float acceleration = 22f;
        public float deceleration = 28f;
        public float turnSharpness = 18f;
        public float gravity = -28f;

        CharacterController controller;
        Vector3 planarVelocity;
        float verticalVelocity;

        void Awake() {
            controller = GetComponent<CharacterController>();
        }

        void Update() {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

            Vector3 input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f) input.Normalize();

            bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            Vector3 target = input * (sprint ? sprintSpeed : walkSpeed);
            float rate = input.sqrMagnitude > .001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, rate * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * Time.deltaTime;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (input.sqrMagnitude > .01f) {
                Quaternion wanted = Quaternion.LookRotation(input, Vector3.up);
                float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, wanted, t);
            }
        }
    }
}
