using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9700)]
    public sealed class HouseCutawayV764 : MonoBehaviour {
        public Transform houseShell;
        public GameObject houseDetails;
        public Vector2 minXZ = new Vector2(-36.25f, 2.62f);
        public Vector2 maxXZ = new Vector2(-21.75f, 15.85f);
        public float wallAlpha = .20f;

        CoziPlayerV57 player;
        readonly List<Renderer> wallRenderers = new List<Renderer>();
        readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        readonly Dictionary<Renderer, Material[]> faded = new Dictionary<Renderer, Material[]>();
        bool inside;
        bool initialized;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            Resolve();
            CacheWalls();
            Apply(true);
        }

        void Resolve() {
            if (!houseShell) {
                GameObject world = GameObject.Find("WORLD EXPANSION V75 - HOUSE A");
                if (world) {
                    Transform t = world.transform.Find("HOUSE A");
                    if (t) houseShell = t;
                    Transform d = world.transform.Find("HOUSE DETAILS");
                    if (d) houseDetails = d.gameObject;
                }
            }
        }

        void CacheWalls() {
            wallRenderers.Clear();
            originals.Clear();
            faded.Clear();
            if (!houseShell) return;
            foreach (Renderer r in houseShell.GetComponentsInChildren<Renderer>(true)) {
                if (!r) continue;
                string n = r.gameObject.name.ToLowerInvariant();
                if (!(n == "wall" || n.Contains("wall cap"))) continue;
                wallRenderers.Add(r);
                Material[] src = r.sharedMaterials;
                originals[r] = src;
                Material[] dst = new Material[src.Length];
                for (int i=0;i<src.Length;i++) dst[i] = MakeTransparent(src[i], wallAlpha);
                faded[r] = dst;
            }
        }

        Material MakeTransparent(Material src, float alpha) {
            if (!src) return null;
            Material m = new Material(src);
            Color c = Color.white;
            if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
            else if (m.HasProperty("_Color")) c = m.GetColor("_Color");
            c.a = alpha;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);

            if (m.HasProperty("_Surface")) {
                m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            } else {
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
            return m;
        }

        bool IsInside() {
            if (!player) return false;
            Vector3 p = player.transform.position;
            return p.x > minXZ.x && p.x < maxXZ.x && p.z > minXZ.y && p.z < maxXZ.y;
        }

        void LateUpdate() { Apply(false); }

        void Apply(bool force) {
            if (!player) return;
            if (!houseShell || !houseDetails) { Resolve(); if (wallRenderers.Count == 0) CacheWalls(); }
            bool now = IsInside();
            if (!force && initialized && now == inside) return;
            initialized = true;
            inside = now;

            // Outside: keep the house shell readable but do not expose its furnished interior.
            // Inside: reveal furniture and fade walls instead of dropping a giant roof over the scene.
            if (houseDetails) houseDetails.SetActive(inside);
            foreach (Renderer r in wallRenderers) {
                if (!r) continue;
                if (inside && faded.TryGetValue(r, out Material[] fm)) r.sharedMaterials = fm;
                else if (originals.TryGetValue(r, out Material[] om)) r.sharedMaterials = om;
            }
        }

        void OnDestroy() {
            foreach (var kv in faded) {
                if (kv.Value == null) continue;
                foreach (Material m in kv.Value) if (m) Destroy(m);
            }
        }
    }

    [DefaultExecutionOrder(9900)]
    public sealed class PlayerVisibilityGuardV764 : MonoBehaviour {
        CoziPlayerV57 player;
        CozyCameraV57 followCamera;
        MeshRenderer[] playerMeshes = Array.Empty<MeshRenderer>();
        float nextRepair;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            Repair();
        }

        void LateUpdate() {
            if (Time.unscaledTime < nextRepair) return;
            nextRepair = Time.unscaledTime + .35f;
            Repair();
        }

        void Repair() {
            if (!player) return;
            if (playerMeshes == null || playerMeshes.Length == 0) playerMeshes = player.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer r in playerMeshes) {
                if (!r) continue;
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("weather") || n.Contains("rain") || n.Contains("snow")) continue;
                if (!r.enabled) r.enabled = true;
            }
            if (!followCamera) followCamera = FindFirstObjectByType<CozyCameraV57>();
            if (followCamera && followCamera.target != player.transform) followCamera.target = player.transform;
        }
    }
}
