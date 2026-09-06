using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class TopDownGameplayV51 : MonoBehaviour {
        [System.Serializable]
        public class LootEntry {
            public GameObject spot;
            public string displayName;
            public string lootText;
        }

        public List<LootEntry> loot = new List<LootEntry>();
        public float interactDistance = 2.4f;

        readonly HashSet<GameObject> searched = new HashSet<GameObject>();
        readonly List<string> inventory = new List<string>();
        LootEntry nearest;
        string flash = "";
        float flashUntil;

        void Update() {
            nearest = null;
            float best = interactDistance * interactDistance;
            Vector3 here = transform.position;

            foreach (LootEntry e in loot) {
                if (e == null || !e.spot || searched.Contains(e.spot)) continue;
                Vector3 d = e.spot.transform.position - here;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr < best) { best = sqr; nearest = e; }
            }

            Keyboard kb = Keyboard.current;
            if (nearest != null && kb != null && kb.eKey.wasPressedThisFrame) Search(nearest);
        }

        void Search(LootEntry e) {
            searched.Add(e.spot);
            string item = string.IsNullOrWhiteSpace(e.lootText) ? "Tomt" : e.lootText;
            if (item != "Tomt") inventory.Add(item);
            flash = e.displayName + ": " + item;
            flashUntil = Time.time + 3.5f;

            Renderer[] rs = e.spot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rs) r.enabled = false;
        }

        void OnGUI() {
            GUIStyle panel = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
            GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 14 };

            GUI.Box(new Rect(16, 16, 255, 78 + inventory.Count * 20), "ROS VIK // PROTOTYP V51", panel);
            GUI.Label(new Rect(28, 48, 220, 24), "WASD: rörelse   Shift: spring", small);
            GUI.Label(new Rect(28, 68, 220, 24), "E: sök   Mushjul: zoom", small);
            for (int i = 0; i < inventory.Count; i++)
                GUI.Label(new Rect(28, 94 + i * 20, 220, 22), "• " + inventory[i], small);

            if (nearest != null) {
                GUI.Box(new Rect(Screen.width * .5f - 155f, Screen.height - 105f, 310f, 48f), "");
                GUI.Label(new Rect(Screen.width * .5f - 150f, Screen.height - 102f, 300f, 42f), "E  Sök " + nearest.displayName, prompt);
            }

            if (Time.time < flashUntil) {
                GUI.Box(new Rect(Screen.width * .5f - 170f, 22f, 340f, 42f), "");
                GUI.Label(new Rect(Screen.width * .5f - 164f, 23f, 328f, 38f), flash, prompt);
            }
        }
    }
}
