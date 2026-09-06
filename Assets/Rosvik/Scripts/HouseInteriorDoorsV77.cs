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
        public Vector3 InteractionPoint => hinge ? hinge.position : transform.position;

        void Awake() {
            Resolve();
            if (hinge) opened = Quaternion.Angle(hinge.localRotation, Quaternion.Euler(closedEuler)) > 18f;
            SetLeafCollision(!opened);
        }

        void OnEnable() {
            Resolve();
            SetLeafCollision(!opened);
        }

        void Resolve() {
            if (!hinge) hinge = transform.Find("door hinge");
            leafColliders = hinge ? hinge.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        }

        public void Toggle() {
            if (animating) return;
            if (!hinge) Resolve();
            if (!hinge) return;
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
            if (!hinge) Resolve();
            if (leafColliders == null || leafColliders.Length == 0) Resolve();
            foreach (Collider c in leafColliders) if (c) c.enabled = value;
        }
    }

    [DefaultExecutionOrder(-4100)]
    public sealed class HouseInteriorDoorNetworkV77 : MonoBehaviour {
        public float interactionDistance = 2.85f;
        CoziPlayerV57 player;
        SurvivalLootTransferV74 lootTransfer;
        HouseInteriorDoorV77 focused;
        readonly List<HouseInteriorDoorV77> doors = new List<HouseInteriorDoorV77>();
        float nextRefresh;
        bool ownsBlock;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            lootTransfer = GetComponent<SurvivalLootTransferV74>();
            Refresh();
        }

        public void Refresh() {
            doors.Clear();
            doors.AddRange(FindObjectsByType<HouseInteriorDoorV77>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        bool OtherModalUiOpen() {
            if (lootTransfer && lootTransfer.IsOpen) return true;
            return false;
        }

        void Update() {
            if (!player) return;
            if (Time.unscaledTime >= nextRefresh) {
                nextRefresh = Time.unscaledTime + .35f;
                Refresh();
            }

            focused = FindClosestDoor();
            if (!focused || OtherModalUiOpen()) return;

            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;

            // Interior doors own this E press for a single frame. This prevents the old
            // generic interaction scanner from consuming the same key while still avoiding
            // the stale externalUiBlocked state that previously made house doors inert.
            player.externalUiBlocked = true;
            ownsBlock = true;
            focused.Toggle();
        }

        HouseInteriorDoorV77 FindClosestDoor() {
            HouseInteriorDoorV77 bestDoor = null;
            float best = interactionDistance * interactionDistance;
            Vector3 here = player.transform.position;
            foreach (HouseInteriorDoorV77 d in doors) {
                if (!d || !d.gameObject.activeInHierarchy || d.IsAnimating) continue;
                Vector3 delta = d.InteractionPoint - here;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq <= best) { best = sq; bestDoor = d; }
            }
            return bestDoor;
        }

        void LateUpdate() {
            if (!ownsBlock || !player) return;
            ownsBlock = false;
            player.externalUiBlocked = false;
        }

        void OnGUI() {
            if (!player || !focused || OtherModalUiOpen()) return;
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
