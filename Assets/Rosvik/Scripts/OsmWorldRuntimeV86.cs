using System;
using UnityEngine;

namespace Rosvik.Blackout {
    [Serializable]
    public sealed class CutawayTargetV86 {
        public GameObject visualRoot;
        public Vector2 minXZ;
        public Vector2 maxXZ;
        public float padding = .45f;
    }

    [DefaultExecutionOrder(3000)]
    public sealed class OsmWorldRuntimeV86 : MonoBehaviour {
        public CutawayTargetV86[] cutaways = Array.Empty<CutawayTargetV86>();
        public string attribution = "© OpenStreetMap contributors · ODbL 1.0";
        CoziPlayerV57 player;

        void Awake() { player = FindFirstObjectByType<CoziPlayerV57>(); Apply(true); }
        void LateUpdate() { if (!player) player = FindFirstObjectByType<CoziPlayerV57>(); Apply(false); }

        void Apply(bool force) {
            if (!player || cutaways == null) return;
            Vector3 p = player.transform.position;
            foreach (CutawayTargetV86 t in cutaways) {
                if (t == null || !t.visualRoot) continue;
                float inset = Mathf.Max(.20f, Mathf.Abs(t.padding));
                bool valid = (t.maxXZ.x - t.minXZ.x) > inset * 2f && (t.maxXZ.y - t.minXZ.y) > inset * 2f;
                bool inside = valid &&
                    p.x > t.minXZ.x + inset && p.x < t.maxXZ.x - inset &&
                    p.z > t.minXZ.y + inset && p.z < t.maxXZ.y - inset;
                bool show = !inside;
                if (force || t.visualRoot.activeSelf != show) t.visualRoot.SetActive(show);
            }
        }

        void OnGUI() {
            if (string.IsNullOrEmpty(attribution)) return;
            GUIStyle s = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.LowerRight,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .014f), 10, 14),
                fontStyle = FontStyle.Normal
            };
            s.normal.textColor = new Color(.78f,.82f,.83f,.72f);
            GUI.Label(new Rect(Screen.width - 370f, Screen.height - 28f, 355f, 20f), attribution, s);
        }
    }
}
