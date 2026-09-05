using UnityEngine;

namespace Rosvik.Blackout {
    [RequireComponent(typeof(CharacterController))]
    public class RosvikPlayerController : MonoBehaviour {
        public float walkSpeed = 3.6f;
        public float sprintSpeed = 5.8f;
        public float turnSpeed = 14f;
        CharacterController cc;

        void Awake() => cc = GetComponent<CharacterController>();

        void Update() {
            // World-locked controls: W is always Rosvik-north, regardless of camera orbit.
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 move = new(input.x, 0f, input.y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            cc.Move(move * speed * Time.deltaTime + Physics.gravity * Time.deltaTime);
            if (move.sqrMagnitude > .01f) {
                Quaternion target = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }
}