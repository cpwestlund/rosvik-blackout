using UnityEngine;

namespace Rosvik.Blackout {
    public class SchoolCutaway : MonoBehaviour {
        public Transform player;
        public Renderer roof;
        public Renderer[] frontWallRenderers;
        public Collider[] frontWallColliders;
        public Vector3 localMin = new(-17f,0f,-5.2f);
        public Vector3 localMax = new(17f,3.5f,5.2f);

        void LateUpdate() {
            if (!player) return;
            Vector3 p = transform.InverseTransformPoint(player.position);
            bool inside = p.x > localMin.x && p.x < localMax.x && p.z > localMin.z && p.z < localMax.z;
            if (roof) roof.enabled = !inside;
            if (frontWallRenderers != null) foreach (var r in frontWallRenderers) if (r) r.enabled = !inside;
            // Important: cutaway is purely visual. Physics never changes here.
            // The entrance gap is physically open in the authored geometry.
        }
    }
}