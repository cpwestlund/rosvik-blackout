using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    [DisallowMultipleComponent]
    public sealed class WinterFootprintsV95 : MonoBehaviour {
        public Material footprintMaterial;
        public float stepSpacing = 0.46f;
        public float footSeparation = 0.17f;
        public float footprintLength = 0.32f;
        public float footprintWidth = 0.14f;
        public float surfaceOffset = 0.02f;
        public int maxFootprints = 220;
        public float raycastHeight = 1.2f;
        public float raycastDistance = 2.5f;

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
            Vector3 current = transform.position;
            Vector3 flatDelta = current - lastStepPosition;
            flatDelta.y = 0f;
            float dist = flatDelta.magnitude;
            if (dist < stepSpacing) return;

            Vector3 dir = flatDelta.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = lastMoveDirection;
            lastMoveDirection = dir;

            int safety = 0;
            while (dist >= stepSpacing && safety++ < 10) {
                Vector3 sample = lastStepPosition + dir * stepSpacing;
                sample.y = current.y;

                if (TryResolveSnowSurface(sample, out Vector3 surfacePoint, out Vector3 normal)) {
                    Vector3 right = Vector3.Cross(normal, dir).normalized;
                    if (right.sqrMagnitude < 0.01f) right = new Vector3(dir.z, 0f, -dir.x).normalized;
                    float side = leftFoot ? -footSeparation : footSeparation;
                    SpawnFootprint(surfacePoint + right * side + normal * surfaceOffset, normal, dir, leftFoot);
                    leftFoot = !leftFoot;
                }

                lastStepPosition = sample;
                flatDelta = current - lastStepPosition;
                flatDelta.y = 0f;
                dist = flatDelta.magnitude;
                if (dist > 0.001f) dir = flatDelta.normalized;
            }
        }

        bool TryResolveSnowSurface(Vector3 p, out Vector3 point, out Vector3 normal) {
            RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * raycastHeight, Vector3.down, raycastDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));

            bool sawBlockingIndoorSurface = false;
            foreach (RaycastHit h in hits) {
                if (!h.collider || h.collider.transform.IsChildOf(transform)) continue;
                string n = h.collider.gameObject.name.ToLowerInvariant();
                bool snowLike = n.Contains("snow") || n.Contains("terrain") || n.Contains("road") || n.Contains("packed") ||
                                n.Contains("asphalt") || n.Contains("ground") || n.Contains("forecourt") || n.Contains("apron");
                if (snowLike && h.normal.y > 0.35f) {
                    point = h.point;
                    normal = h.normal;
                    return true;
                }
                if (h.normal.y > 0.35f && (n.Contains("floor") || n.Contains("interior") || n.Contains("room") || n.Contains("hall")))
                    sawBlockingIndoorSurface = true;
            }

            // The V95 soft snow mesh may momentarily have no physics hit while the scene finishes loading.
            // Only use the flat fallback when there was no obvious indoor floor underneath the player.
            if (!sawBlockingIndoorSurface && Mathf.Abs(p.y) < 0.45f) {
                point = new Vector3(p.x, 0.018f, p.z);
                normal = Vector3.up;
                return true;
            }

            point = default;
            normal = Vector3.up;
            return false;
        }

        void SpawnFootprint(Vector3 pos, Vector3 normal, Vector3 direction, bool left) {
            if (!footprintMaterial) return;
            GameObject g = new GameObject(left ? "left boot print" : "right boot print");
            g.transform.position = pos;
            Vector3 tangent = Vector3.ProjectOnPlane(direction, normal).normalized;
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.forward;
            g.transform.rotation = Quaternion.LookRotation(tangent, normal) * Quaternion.Euler(0f, left ? -5f : 5f, 0f);
            g.transform.localScale = new Vector3(footprintWidth / 0.14f, 1f, footprintLength / 0.32f);

            MeshFilter mf = g.AddComponent<MeshFilter>();
            mf.sharedMesh = footprintMesh;
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = footprintMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            prints.Enqueue(g);
            while (prints.Count > Mathf.Max(40, maxFootprints)) {
                GameObject old = prints.Dequeue();
                if (old) Destroy(old);
            }
        }

        static Mesh BuildBootMesh() {
            const float w = 0.14f;
            const float l = 0.32f;
            Vector3[] v = {
                new Vector3(-w*.36f,0,-l*.50f), new Vector3(w*.36f,0,-l*.50f),
                new Vector3(w*.48f,0,-l*.26f),  new Vector3(w*.50f,0,l*.20f),
                new Vector3(w*.34f,0,l*.48f),   new Vector3(-w*.34f,0,l*.48f),
                new Vector3(-w*.50f,0,l*.20f),  new Vector3(-w*.48f,0,-l*.26f)
            };
            int[] t = {0,2,1, 0,7,2, 7,3,2, 7,6,3, 6,4,3, 6,5,4};
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
