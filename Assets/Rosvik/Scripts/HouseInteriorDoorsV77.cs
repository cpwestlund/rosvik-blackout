using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public sealed class HouseInteriorDoorV77 : MonoBehaviour {
        public string displayName = "dörren";
        public Transform hinge;
        public Vector3 closedEuler = Vector3.zero;
        public Vector3 openEuler = new Vector3(0f, 92f, 0f);
        public float animationTime = .24f;

        bool opened;
        bool animating;
        Collider[] leafColliders = Array.Empty<Collider>();

        public bool IsOpen => opened;
        public bool IsAnimating => animating;

        void Awake() {
            Resolve();
            if (hinge) opened = Quaternion.Angle(hinge.localRotation, Quaternion.Euler(closedEuler)) > 18f;
            SetLeafCollision(!opened);
        }

        void Resolve() {
            if (!hinge) hinge = transform.Find("door hinge");
            leafColliders = hinge ? hinge.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        }

        public void Toggle() {
            if (animating || !hinge) return;
            StartCoroutine(Animate(!opened));
        }

        IEnumerator Animate(bool targetOpen) {
            animating = true;
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
        }

        void SetLeafCollision(bool value) {
            if (leafColliders == null || leafColliders.Length == 0) Resolve();
            foreach (Collider c in leafColliders) if (c) c.enabled = value;
        }
    }

    [DefaultExecutionOrder(-3900)]
    public sealed class HouseInteriorDoorNetworkV77 : MonoBehaviour {
        public float interactionDistance = 2.25f;
        CoziPlayerV57 player;
        HouseInteriorDoorV77 focused;
        readonly List<HouseInteriorDoorV77> doors = new List<HouseInteriorDoorV77>();
        float nextRefresh;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            Refresh();
        }

        public void Refresh() {
            doors.Clear();
            doors.AddRange(FindObjectsByType<HouseInteriorDoorV77>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        void Update() {
            if (!player || player.externalUiBlocked) { focused = null; return; }
            if (Time.time >= nextRefresh) { nextRefresh = Time.time + .75f; if (doors.Count == 0) Refresh(); }

            focused = null;
            float best = interactionDistance * interactionDistance;
            Vector3 here = player.transform.position;
            foreach (HouseInteriorDoorV77 d in doors) {
                if (!d || !d.gameObject.activeInHierarchy || d.IsAnimating) continue;
                Vector3 delta = d.transform.position - here; delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq <= best) { best = sq; focused = d; }
            }
            Keyboard kb = Keyboard.current;
            if (focused && kb != null && kb.eKey.wasPressedThisFrame) focused.Toggle();
        }

        void OnGUI() {
            if (!player || !focused || player.externalUiBlocked) return;
            Rect r = new Rect(Screen.width * .5f - 180f, Screen.height - 66f, 360f, 36f);
            Color old = GUI.color;
            GUI.color = new Color(.018f,.024f,.021f,.95f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUIStyle s = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            s.normal.textColor = new Color(.92f,.90f,.82f);
            GUI.Label(r, "E   " + (focused.IsOpen ? "STÄNG " : "ÖPPNA ") + focused.displayName.ToUpperInvariant(), s);
        }
    }
}
