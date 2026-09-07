using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    [DisallowMultipleComponent]
    public sealed class WinterFootprintsV95 : MonoBehaviour {
        public Material footprintMaterial;
        public float stepSpacing = 0.58f;
        public float footSeparation = 0.17f;
        public float footprintLength = 0.32f;
        public float footprintWidth = 0.14f;
        public float surfaceOffset = 0.018f;
        public int maxFootprints = 140;
        public float raycastHeight = 1.1f;
        public float raycastDistance = 2.2f;

        readonly Queue<GameObject> prints = new Queue<GameObject>();
        Vector3 lastStepPosition;
        Vector3 lastMoveDirection = Vector3.forward;
        bool leftFoot;
        static Mesh footprintMesh;

        void Start() {
            lastStepPosition = transform.position;
            if (footprintMesh == null) footprintMesh = BuildBootMesh();
        }

        void Update() {
            Vector3 p = transform.position;
            Vector3 delta = p - lastStepPosition;
            delta.y = 0f;
            if (delta.magnitude < stepSpacing) return;

            Vector3 dir = delta.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = lastMoveDirection;
            lastMoveDirection = dir;

            if (TryFindSnowSurface(p, out RaycastHit hit)) {
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);
                float side = leftFoot ? -footSeparation : footSeparation;
                Vector3 pos = hit.point + right * side + hit.normal * surfaceOffset;
                SpawnFootprint(pos, hit.normal, dir, leftFoot);
                leftFoot = !leftFoot;
            }
            lastStepPosition = p;
        }

        bool TryFindSnowSurface(Vector3 p, out RaycastHit chosen) {
            RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * raycastHeight, Vector3.down, raycastDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in hits) {
                if (!h.collider) continue;
                string n = h.collider.gameObject.name.ToLowerInvariant();
                if (n.Contains("snow") || n.Contains("terrain") || n.Contains("road") || n.Contains("packed") || n.Contains("asphalt") || n.Contains("ground") || n.Contains("forecourt") || n.Contains("apron")) {
                    chosen = h;
                    return true;
                }
            }
            chosen = default;
            return false;
        }

        void SpawnFootprint(Vector3 pos, Vector3 normal, Vector3 direction, bool left) {
            GameObject g = new GameObject(left ? "left boot print" : "right boot print");
            g.transform.position = pos;
            Quaternion face = Quaternion.LookRotation(direction, normal);
            g.transform.rotation = face * Quaternion.Euler(90f, left ? -4f : 4f, 0f);
            g.transform.localScale = new Vector3(footprintWidth / 0.14f, footprintLength / 0.32f, 1f);

            MeshFilter mf = g.AddComponent<MeshFilter>();
            mf.sharedMesh = footprintMesh;
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = footprintMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            prints.Enqueue(g);
            while (prints.Count > Mathf.Max(20, maxFootprints)) {
                GameObject old = prints.Dequeue();
                if (old) Destroy(old);
            }
        }

        static Mesh BuildBootMesh() {
            const float w = 0.14f;
            const float l = 0.32f;
            Vector3[] v = {
                new Vector3(-w*.36f,-l*.50f,0), new Vector3(w*.36f,-l*.50f,0),
                new Vector3(w*.48f,-l*.26f,0),  new Vector3(w*.50f,l*.20f,0),
                new Vector3(w*.34f,l*.48f,0),   new Vector3(-w*.34f,l*.48f,0),
                new Vector3(-w*.50f,l*.20f,0),  new Vector3(-w*.48f,-l*.26f,0)
            };
            int[] t = {0,1,2, 0,2,7, 7,2,3, 7,3,6, 6,3,4, 6,4,5};
            Vector2[] uv = {
                new Vector2(.14f,0),new Vector2(.86f,0),new Vector2(.98f,.24f),new Vector2(1,.70f),
                new Vector2(.82f,1),new Vector2(.18f,1),new Vector2(0,.70f),new Vector2(.02f,.24f)
            };
            Mesh m = new Mesh { name = "V95 boot footprint" };
            m.vertices = v; m.triangles = t; m.uv = uv;
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SnowfallFollowV95 : MonoBehaviour {
        public Transform target;
        public float height = 8.5f;
        public Vector3 windOffset = new Vector3(-1.0f, 0f, 0.35f);

        void LateUpdate() {
            if (!target) return;
            transform.position = target.position + Vector3.up * height + windOffset;
        }
    }
}
