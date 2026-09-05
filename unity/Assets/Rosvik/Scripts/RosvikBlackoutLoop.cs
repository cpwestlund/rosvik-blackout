using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    /// <summary>
    /// First real gameplay loop for the Rosvik prototype: find three useful parts around
    /// the real school/sports area, then return to the reserve-power cabinet and restore
    /// emergency lighting. Kept deliberately dependency-light so it survives scene passes.
    /// </summary>
    public sealed class RosvikBlackoutLoop : MonoBehaviour {
        public Transform player;
        public Transform restoreStation;
        public Transform[] pickups;
        public string[] pickupNames;
        public Light[] emergencyLights;
        public float pickupDistance = 2.4f;
        public float stationDistance = 3.0f;

        bool[] collected;
        bool restored;
        GUIStyle titleStyle, bodyStyle, promptStyle, doneStyle;

        public bool PowerRestored => restored;

        void Awake() {
            collected = new bool[pickups != null ? pickups.Length : 0];
            SetLights(false);
        }

        void Update() {
            if (!player || Keyboard.current == null) return;
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            int near = NearestPickup();
            if (near >= 0) {
                Collect(near);
                return;
            }

            if (!restored && AllCollected() && restoreStation && FlatDistance(player.position, restoreStation.position) <= stationDistance)
                RestorePower();
        }

        int NearestPickup() {
            if (pickups == null || collected == null) return -1;
            int best = -1;
            float bestD = pickupDistance;
            for (int i = 0; i < pickups.Length; i++) {
                if (collected[i] || !pickups[i]) continue;
                float d = FlatDistance(player.position, pickups[i].position);
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void Collect(int index) {
            collected[index] = true;
            if (pickups[index]) pickups[index].gameObject.SetActive(false);
        }

        bool AllCollected() {
            if (collected == null || collected.Length == 0) return false;
            for (int i = 0; i < collected.Length; i++) if (!collected[i]) return false;
            return true;
        }

        int CollectedCount() {
            if (collected == null) return 0;
            int n = 0; for (int i = 0; i < collected.Length; i++) if (collected[i]) n++;
            return n;
        }

        void RestorePower() {
            restored = true;
            SetLights(true);
        }

        void SetLights(bool on) {
            if (emergencyLights == null) return;
            foreach (Light l in emergencyLights) if (l) l.enabled = on;
        }

        string PickupName(int i) {
            if (pickupNames != null && i < pickupNames.Length && !string.IsNullOrEmpty(pickupNames[i])) return pickupNames[i];
            return "Reservdel";
        }

        void EnsureStyles() {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.95f,.90f,.75f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            bodyStyle.normal.textColor = new Color(.90f,.89f,.83f);
            doneStyle = new GUIStyle(bodyStyle);
            doneStyle.normal.textColor = new Color(.62f,.82f,.58f);
            promptStyle = new GUIStyle(bodyStyle) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            promptStyle.normal.textColor = new Color(.98f,.94f,.78f);
        }

        void OnGUI() {
            if (!player) return;
            EnsureStyles();

            Rect panel = new Rect(18f, 18f, 290f, 128f);
            Color old = GUI.color;
            GUI.color = new Color(.055f,.065f,.06f,.82f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = old;

            GUI.Label(new Rect(32f, 27f, 250f, 24f), restored ? "RESERVKRAFT ONLINE" : "STRÖMAVBROTT", titleStyle);
            if (restored) {
                GUI.Label(new Rect(32f, 58f, 250f, 50f), "Skolområdet har nödström.\nNästa steg: säkra området.", doneStyle);
            } else {
                int total = pickups != null ? pickups.Length : 0;
                GUI.Label(new Rect(32f, 55f, 250f, 22f), "Hitta reservdelar  " + CollectedCount() + "/" + total, bodyStyle);
                float y = 78f;
                for (int i = 0; i < total && i < 3; i++) {
                    string mark = (collected != null && collected[i]) ? "✓ " : "• ";
                    GUI.Label(new Rect(34f, y, 250f, 19f), mark + PickupName(i), collected != null && collected[i] ? doneStyle : bodyStyle);
                    y += 18f;
                }
                if (AllCollected()) GUI.Label(new Rect(32f, 112f, 255f, 22f), "Återvänd till reservkraften vid skolan.", doneStyle);
            }

            string prompt = InteractionPrompt();
            if (!string.IsNullOrEmpty(prompt)) {
                Rect pr = new Rect(Screen.width * .5f - 210f, Screen.height - 82f, 420f, 38f);
                GUI.color = new Color(.04f,.045f,.04f,.82f); GUI.Box(pr, GUIContent.none); GUI.color = old;
                GUI.Label(pr, prompt, promptStyle);
            }
        }

        string InteractionPrompt() {
            int near = NearestPickup();
            if (near >= 0) return "E  —  Plocka upp " + PickupName(near);
            if (!restored && AllCollected() && restoreStation && FlatDistance(player.position, restoreStation.position) <= stationDistance)
                return "E  —  Starta reservkraften";
            return string.Empty;
        }

        static float FlatDistance(Vector3 a, Vector3 b) {
            a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
        }
    }
}
