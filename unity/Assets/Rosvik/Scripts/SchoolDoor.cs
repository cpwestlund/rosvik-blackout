using UnityEngine;

namespace Rosvik.Blackout {
    public class SchoolDoor : MonoBehaviour {
        public Transform doorLeaf;
        public float openAngle = 100f, speed = 5f;
        bool open;
        Quaternion closed, opened;

        void Start() {
            if (!doorLeaf) doorLeaf = transform;
            closed = doorLeaf.localRotation;
            opened = closed * Quaternion.Euler(0, openAngle, 0);
        }
        void Update() {
            doorLeaf.localRotation = Quaternion.Slerp(doorLeaf.localRotation, open ? opened : closed, speed * Time.deltaTime);
        }
        void OnTriggerEnter(Collider other) { if (other.GetComponent<RosvikPlayerController>()) open = true; }
        void OnTriggerExit(Collider other) { if (other.GetComponent<RosvikPlayerController>()) open = false; }
    }
}