using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class TopDownGameplayV53 : MonoBehaviour {
        public enum SpotKind {
            Loot,
            SchoolEntrance,
            InteriorExit,
            OpenDoor,
            LockedDoor,
            SportsHallDoor
        }

        [System.Serializable]
        public class Spot {
            public GameObject spot;
            public string displayName;
            public string itemName;
            public SpotKind kind;
            public string requiredItem;
            public GameObject gate;
            public Vector3 teleportTarget;
        }

        public List<Spot> spots = new List<Spot>();
        public float interactDistance = 2.15f;

        readonly HashSet<GameObject> searched = new HashSet<GameObject>();
        readonly HashSet<GameObject> opened = new HashSet<GameObject>();
        readonly List<string> inventory = new List<string>();

        Spot nearest;
        string flash = "";
        float flashUntil;
        bool insideSchool;
        bool sportsHallUnlocked;

        void Update() {
            nearest = null;
            float best = interactDistance * interactDistance;
            Vector3 here = transform.position;

            foreach (Spot s in spots) {
                if (s == null || !s.spot || !s.spot.activeInHierarchy) continue;
                if (s.kind == SpotKind.Loot && searched.Contains(s.spot)) continue;
                if ((s.kind == SpotKind.OpenDoor || s.kind == SpotKind.LockedDoor || s.kind == SpotKind.SportsHallDoor) && opened.Contains(s.spot)) continue;

                Vector3 d = s.spot.transform.position - here;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr < best) {
                    best = sqr;
                    nearest = s;
                }
            }

            Keyboard kb = Keyboard.current;
            if (nearest != null && kb != null && kb.eKey.wasPressedThisFrame)
                Interact(nearest);
        }

        void Interact(Spot s) {
            switch (s.kind) {
                case SpotKind.Loot:
                    Search(s);
                    break;
                case SpotKind.SchoolEntrance:
                    EnterSchool(s);
                    break;
                case SpotKind.InteriorExit:
                    Teleport(s.teleportTarget);
                    insideSchool = false;
                    Flash("Du går ut på skolgården.", 2.8f);
                    break;
                case SpotKind.OpenDoor:
                    OpenGate(s, "Dörren öppnas.");
                    break;
                case SpotKind.LockedDoor:
                    if (HasItem(s.requiredItem)) OpenGate(s, "Du låser upp " + s.displayName + ".");
                    else Flash(s.displayName + " är låst. Du behöver " + s.requiredItem + ".", 3.5f);
                    break;
                case SpotKind.SportsHallDoor:
                    if (HasItem(s.requiredItem)) {
                        OpenGate(s, "Huvudnyckeln passar. Dörren mot sporthallen är upplåst.");
                        sportsHallUnlocked = true;
                    } else {
                        Flash("Dörren mot sporthallen är låst. Hitta huvudnyckeln.", 3.5f);
                    }
                    break;
            }
        }

        void Search(Spot s) {
            searched.Add(s.spot);
            if (!string.IsNullOrWhiteSpace(s.itemName) && !HasItem(s.itemName)) inventory.Add(s.itemName);
            string item = string.IsNullOrWhiteSpace(s.itemName) ? "Tomt" : s.itemName;
            Flash(s.displayName + ": " + item, 3.0f);
            SetVisible(s.spot, false);
        }

        void EnterSchool(Spot s) {
            if (!HasItem("Ficklampa") || !HasItem("Säkring")) {
                Flash("Skolentrén är låst. Hitta Ficklampa och Säkring först.", 3.5f);
                return;
            }
            Teleport(s.teleportTarget);
            insideSchool = true;
            Flash("Rosviks skola. Strömmen är borta. Ficklampan blir viktig här inne.", 4.2f);
        }

        void OpenGate(Spot s, string message) {
            opened.Add(s.spot);
            if (s.gate) SetVisible(s.gate, false);
            SetVisible(s.spot, false);
            Flash(message, 3.2f);
        }

        bool HasItem(string item) {
            if (string.IsNullOrWhiteSpace(item)) return true;
            return inventory.Contains(item);
        }

        void Teleport(Vector3 target) {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.position = target;
            if (cc) cc.enabled = true;
        }

        static void SetVisible(GameObject go, bool visible) {
            if (!go) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = visible;
        }

        void Flash(string text, float seconds) {
            flash = text;
            flashUntil = Time.time + seconds;
        }

        string Objective() {
            if (sportsHallUnlocked) return "MÅL: Dörren mot sporthallen är upplåst";
            if (!insideSchool) {
                bool lamp = HasItem("Ficklampa");
                bool fuse = HasItem("Säkring");
                if (!lamp || !fuse) return "MÅL: Hitta Ficklampa + Säkring";
                return "MÅL: Gå in i Rosviks skola";
            }
            if (!HasItem("Nyckelknippa")) return "MÅL: Sök klassrummet efter en nyckel";
            if (!HasItem("Huvudnyckel")) return "MÅL: Lås upp personalrummet och sök där";
            return "MÅL: Öppna dörren mot sporthallen";
        }

        void OnGUI() {
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
            GUI.Box(new Rect(18,18,350,panelH), "");
            GUI.Label(new Rect(32,27,320,26), "ROSVIK // STRÖMAVBROTT", title);
            GUI.Label(new Rect(32,55,320,22), Objective(), text);
            GUI.Label(new Rect(32,78,320,22), "WASD rörelse   Shift spring   E interagera", text);
            for (int i=0;i<inventory.Count;i++)
                GUI.Label(new Rect(32,104+i*20,310,20), "• " + inventory[i], text);

            if (nearest != null) {
                string action;
                switch (nearest.kind) {
                    case SpotKind.Loot: action = "E  Sök " + nearest.displayName; break;
                    case SpotKind.SchoolEntrance: action = "E  Gå in i skolan"; break;
                    case SpotKind.InteriorExit: action = "E  Gå ut"; break;
                    case SpotKind.OpenDoor: action = "E  Öppna " + nearest.displayName; break;
                    default: action = "E  Försök öppna " + nearest.displayName; break;
                }
                GUI.Box(new Rect(Screen.width*.5f-210f, Screen.height-92f, 420f, 48f), "");
                GUI.Label(new Rect(Screen.width*.5f-205f, Screen.height-89f, 410f, 42f), action, prompt);
            }

            if (Time.time < flashUntil) {
                GUI.Box(new Rect(Screen.width*.5f-280f, 22f, 560f, 48f), "");
                GUI.Label(new Rect(Screen.width*.5f-272f, 24f, 544f, 42f), flash, prompt);
            }
        }
    }
}
