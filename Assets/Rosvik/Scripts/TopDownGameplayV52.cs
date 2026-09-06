using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class TopDownGameplayV52 : MonoBehaviour {
        [System.Serializable]
        public class Spot {
            public GameObject spot;
            public string displayName;
            public string itemName;
            public bool requiredForDoor;
            public bool isDoor;
        }

        public List<Spot> spots = new List<Spot>();
        public float interactDistance = 2.15f;

        readonly HashSet<GameObject> searched = new HashSet<GameObject>();
        readonly List<string> inventory = new List<string>();
        Spot nearest;
        string flash = "";
        float flashUntil;
        bool missionComplete;

        void Update() {
            nearest = null;
            float best = interactDistance * interactDistance;
            Vector3 here = transform.position;

            foreach (Spot e in spots) {
                if (e == null || !e.spot) continue;
                if (!e.isDoor && searched.Contains(e.spot)) continue;
                if (e.isDoor && missionComplete) continue;

                Vector3 d = e.spot.transform.position - here;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr < best) {
                    best = sqr;
                    nearest = e;
                }
            }

            Keyboard kb = Keyboard.current;
            if (nearest != null && kb != null && kb.eKey.wasPressedThisFrame)
                Interact(nearest);
        }

        void Interact(Spot e) {
            if (e.isDoor) {
                if (RequiredFound() >= RequiredTotal()) {
                    missionComplete = true;
                    flash = "Skolentrén är upplåst. Nästa steg: gå in i skolan.";
                    flashUntil = Time.time + 5f;
                    SetSpotVisible(e.spot, false);
                } else {
                    flash = "Dörren är låst. Hitta ficklampan och säkringen först.";
                    flashUntil = Time.time + 3.5f;
                }
                return;
            }

            searched.Add(e.spot);
            if (!string.IsNullOrWhiteSpace(e.itemName)) inventory.Add(e.itemName);
            flash = e.displayName + ": " + (string.IsNullOrWhiteSpace(e.itemName) ? "Tomt" : e.itemName);
            flashUntil = Time.time + 3.2f;
            SetSpotVisible(e.spot, false);
        }

        int RequiredTotal() {
            int count = 0;
            foreach (Spot e in spots) if (e != null && e.requiredForDoor && !e.isDoor) count++;
            return count;
        }

        int RequiredFound() {
            int count = 0;
            foreach (Spot e in spots)
                if (e != null && e.requiredForDoor && !e.isDoor && e.spot && searched.Contains(e.spot)) count++;
            return count;
        }

        static void SetSpotVisible(GameObject go, bool visible) {
            if (!go) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = visible;
        }

        void OnGUI() {
            int required = RequiredTotal();
            int found = RequiredFound();

            GUIStyle title = new GUIStyle(GUI.skin.label) {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.94f,.91f,.81f) }
            };
            GUIStyle text = new GUIStyle(GUI.skin.label) {
                fontSize = 14,
                normal = { textColor = new Color(.90f,.90f,.86f) }
            };
            GUIStyle prompt = new GUIStyle(GUI.skin.label) {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            float panelH = 112f + inventory.Count * 20f;
            GUI.Box(new Rect(18,18,320,panelH), "");
            GUI.Label(new Rect(32,27,290,26), "ROSVIK // STRÖMAVBROTT", title);

            string objective;
            if (missionComplete) objective = "MÅL: Skolentrén är upplåst";
            else if (required > 0) objective = "MÅL: Hitta utrustning  " + found + "/" + required;
            else objective = "MÅL: Sök området";
            GUI.Label(new Rect(32,55,290,22), objective, text);
            GUI.Label(new Rect(32,78,290,22), "WASD rörelse   Shift spring   E interagera", text);

            for (int i=0;i<inventory.Count;i++)
                GUI.Label(new Rect(32,104+i*20,285,20), "• " + inventory[i], text);

            if (nearest != null) {
                string action = nearest.isDoor ? "E  Försök öppna skolentrén" : "E  Sök " + nearest.displayName;
                GUI.Box(new Rect(Screen.width*.5f-190f, Screen.height-92f, 380f, 48f), "");
                GUI.Label(new Rect(Screen.width*.5f-185f, Screen.height-89f, 370f, 42f), action, prompt);
            }

            if (Time.time < flashUntil) {
                GUI.Box(new Rect(Screen.width*.5f-245f, 22f, 490f, 44f), "");
                GUI.Label(new Rect(Screen.width*.5f-238f, 24f, 476f, 40f), flash, prompt);
            }
        }
    }
}
