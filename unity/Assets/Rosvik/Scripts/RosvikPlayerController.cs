using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(CharacterController))]
    public class RosvikPlayerController : MonoBehaviour {
        public float walkSpeed = 3.6f;
        public float sprintSpeed = 5.8f;
        public float turnSpeed = 14f;
        CharacterController cc;

        void Awake() => cc = GetComponent<CharacterController>();

        void Update() {
            var kb = Keyboard.current;
            if (kb == null) return;

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector3 move = new(x, 0f, z);
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 gravity = Physics.gravity;
            cc.Move((move * speed + gravity) * Time.deltaTime);

            if (move.sqrMagnitude > .01f) {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }
}