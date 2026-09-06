using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(-4000)]
    public sealed class HouseFrontDoorV763 : MonoBehaviour {
        public Transform hinge;
        public Vector3 closedEuler = Vector3.zero;
        public Vector3 openEuler = new Vector3(0f, 105f, 0f);
        public float interactionDistance = 3.4f;
        public float animationTime = .28f;

        CoziPlayerV57 player;
        bool opened;
        bool animating;
        Collider[] leafColliders = Array.Empty<Collider>();

        public bool IsOpen => opened;

        void Awake() {
            player = FindFirstObjectByType<CoziPlayerV57>();
            ResolveHinge();
            CacheLeafColliders();
            if (hinge) opened = Quaternion.Angle(hinge.localRotation, Quaternion.Euler(closedEuler)) > 20f;
            SetLeafCollision(!opened);
        }

        void ResolveHinge() {
            if (hinge) return;
            Transform t = transform.Find("door hinge");
            if (t) hinge = t;
        }

        void CacheLeafColliders() {
            if (!hinge) { leafColliders = Array.Empty<Collider>(); return; }
            leafColliders = hinge.GetComponentsInChildren<Collider>(true);
        }

        void Update() {
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            if (!hinge) { ResolveHinge(); CacheLeafColliders(); }
            if (!player || !hinge || animating || player.externalUiBlocked) return;

            Vector3 d = transform.position - player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > interactionDistance * interactionDistance) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) StartCoroutine(ToggleRoutine(!opened));
        }

        IEnumerator ToggleRoutine(bool targetOpen) {
            animating = true;
            // The leaf never blocks the player while it is moving.
            SetLeafCollision(false);
            Quaternion from = hinge.localRotation;
            Quaternion to = Quaternion.Euler(targetOpen ? openEuler : closedEuler);
            float t = 0f;
            while (t < 1f) {
                t += Time.deltaTime / Mathf.Max(.08f, animationTime);
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                hinge.localRotation = Quaternion.Slerp(from, to, e);
                yield return null;
            }
            hinge.localRotation = to;
            opened = targetOpen;
            SetLeafCollision(!opened);
            animating = false;
            if (player) player.ShowToast(opened ? "Ytterdörren öppnades" : "Ytterdörren stängdes", 1.2f);
        }

        void SetLeafCollision(bool enabled) {
            if (leafColliders == null || leafColliders.Length == 0) CacheLeafColliders();
            foreach (Collider c in leafColliders) if (c) c.enabled = enabled;
        }

        void OnGUI() {
            if (!player || !hinge || player.externalUiBlocked) return;
            Vector3 d = transform.position - player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > interactionDistance * interactionDistance) return;

            Rect r = new Rect(Screen.width * .5f - 205f, Screen.height - 68f, 410f, 38f);
            Color old = GUI.color;
            GUI.color = new Color(.018f, .024f, .021f, .96f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUIStyle s = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            s.normal.textColor = new Color(.93f, .91f, .82f);
            string text = animating ? "Ytterdörr..." : "E   " + (opened ? "STÄNG YTTERDÖRREN" : "ÖPPNA YTTERDÖRREN");
            GUI.Label(r, text, s);
        }
    }

    [DefaultExecutionOrder(-3500)]
    public sealed class HouseLootOcclusionV763 : MonoBehaviour {
        public Vector2 minXZ = new Vector2(-36.25f, 2.55f);
        public Vector2 maxXZ = new Vector2(-21.75f, 15.85f);
        public float enterZ = 2.58f;

        CoziPlayerV57 player;
        readonly List<GameObject> houseLootRoots = new List<GameObject>();
        bool lastInside;
        bool initialized;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            Refresh();
            Apply(true);
        }

        public void Refresh() {
            houseLootRoots.Clear();
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!c) continue;
                Vector3 p = c.transform.position;
                if (p.x > minXZ.x && p.x < maxXZ.x && p.z > minXZ.y && p.z < maxXZ.y)
                    houseLootRoots.Add(c.gameObject);
            }
        }

        bool Inside() {
            if (!player) return false;
            Vector3 p = player.transform.position;
            return p.x > minXZ.x && p.x < maxXZ.x && p.z > enterZ && p.z < maxXZ.y;
        }

        void LateUpdate() { Apply(false); }

        void Apply(bool force) {
            if (!player) return;
            bool inside = Inside();
            if (!force && initialized && inside == lastInside) return;
            initialized = true;
            lastInside = inside;
            if (houseLootRoots.Count == 0) Refresh();
            foreach (GameObject g in houseLootRoots) if (g) g.SetActive(inside);
        }
    }
}
