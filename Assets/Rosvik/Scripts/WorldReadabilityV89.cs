using System;
using UnityEngine;

namespace Rosvik.Blackout {
    [Serializable]
    public sealed class LandmarkZoneV89 {
        public string displayName;
        public GameObject buildingRoot;
        public Vector2 minXZ;
        public Vector2 maxXZ;
        public float nearPadding = 5.5f;
    }

    [DefaultExecutionOrder(3200)]
    public sealed class WorldReadabilityV89 : MonoBehaviour {
        public LandmarkZoneV89[] landmarks = Array.Empty<LandmarkZoneV89>();
        public string areaName = "ROSVIK";
        CoziPlayerV57 player;
        GUIStyle titleStyle, subStyle;

        void Awake() {
            player = FindFirstObjectByType<CoziPlayerV57>();
            ApplyRoofState(true);
        }

        void LateUpdate() {
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            ApplyRoofState(false);
        }

        void ApplyRoofState(bool force) {
            if (!player || landmarks == null) return;
            Vector3 p = player.transform.position;
            foreach (var z in landmarks) {
                if (z == null || !z.buildingRoot) continue;
                float dx = p.x < z.minXZ.x ? z.minXZ.x - p.x : p.x > z.maxXZ.x ? p.x - z.maxXZ.x : 0f;
                float dz = p.z < z.minXZ.y ? z.minXZ.y - p.z : p.z > z.maxXZ.y ? p.z - z.maxXZ.y : 0f;
                bool near = dx <= z.nearPadding && dz <= z.nearPadding;
                foreach (Renderer r in z.buildingRoot.GetComponentsInChildren<Renderer>(true)) {
                    if (!r) continue;
                    string n = r.gameObject.name.ToLowerInvariant();
                    bool roof = n.Contains("roof") || n.Contains("snow cap") || n.Contains("canopy snow");
                    if (roof && (force || r.enabled == near)) r.enabled = !near;
                }
            }
        }

        void OnGUI() {
            if (!player || landmarks == null || landmarks.Length == 0) return;
            LandmarkZoneV89 best = null;
            float bestD = float.MaxValue;
            Vector3 p = player.transform.position;
            foreach (var z in landmarks) {
                if (z == null) continue;
                float cx = (z.minXZ.x + z.maxXZ.x) * .5f;
                float cz = (z.minXZ.y + z.maxXZ.y) * .5f;
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(cx, cz));
                if (d < bestD) { bestD = d; best = z; }
            }
            if (best == null) return;

            if (titleStyle == null) {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .021f), 14, 23), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = new Color(.95f,.94f,.88f,.96f);
                subStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .014f), 10, 15), alignment = TextAnchor.MiddleCenter };
                subStyle.normal.textColor = new Color(.80f,.85f,.86f,.90f);
            }

            float w = Mathf.Min(420f, Screen.width * .34f);
            Rect bg = new Rect((Screen.width - w) * .5f, Screen.height - 72f, w, 50f);
            Color old = GUI.color;
            GUI.color = new Color(.035f,.055f,.058f,.76f);
            GUI.DrawTexture(bg, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(bg.x, bg.y + 3f, bg.width, 24f), best.displayName, titleStyle);
            string suffix = bestD < 8f ? "DU ÄR HÄR" : Mathf.RoundToInt(bestD) + " m";
            GUI.Label(new Rect(bg.x, bg.y + 25f, bg.width, 19f), suffix, subStyle);
        }
    }
}
