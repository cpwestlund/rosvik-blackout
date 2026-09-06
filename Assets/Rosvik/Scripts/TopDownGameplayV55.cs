using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class TopDownGameplayV55 : MonoBehaviour {
        public enum SpotKind {
            Loot,
            SchoolEntrance,
            InteriorExit,
            Door,
            LockedDoor,
            Cabinet,
            LockedCabinet,
            SportsHallDoor
        }

        [System.Serializable]
        public class Spot {
            public GameObject spot;
            public GameObject marker;
            public string displayName;
            public string itemName;
            public string requiredItem;
            public SpotKind kind;
            public float interactRadius = 2.4f;
            public Transform movingPart;
            public Transform movingPart2;
            public Vector3 closedEuler;
            public Vector3 openEuler;
            public Vector3 closedEuler2;
            public Vector3 openEuler2;
            public Vector3 teleportTarget;
        }

        public List<Spot> spots = new List<Spot>();
        public float defaultInteractDistance = 2.4f;
        public float animationTime = .22f;

        readonly HashSet<GameObject> searched = new HashSet<GameObject>();
        readonly Dictionary<GameObject, bool> openState = new Dictionary<GameObject, bool>();
        readonly List<string> inventory = new List<string>();

        Spot nearest;
        string flash = "";
        float flashUntil;
        bool insideSchool;
        bool sportsHallReached;
        bool busy;

        void Awake() {
            foreach (Spot s in spots) {
                if (s == null || !s.spot) continue;
                openState[s.spot] = false;
                SetMarker(s, false);
            }
        }

        void Update() {
            FindNearest();
            Keyboard kb = Keyboard.current;
            if (!busy && nearest != null && kb != null && kb.eKey.wasPressedThisFrame)
                Interact(nearest);
        }

        void FindNearest() {
            Spot old = nearest;
            nearest = null;
            float best = float.MaxValue;
            Vector3 here = transform.position;

            foreach (Spot s in spots) {
                if (s == null || !s.spot || !s.spot.activeInHierarchy) continue;
                if (s.kind == SpotKind.Loot && searched.Contains(s.spot)) continue;

                Vector3 d = s.spot.transform.position - here;
                d.y = 0f;
                float radius = s.interactRadius > .1f ? s.interactRadius : defaultInteractDistance;
                float sqr = d.sqrMagnitude;
                if (sqr <= radius * radius && sqr < best) {
                    best = sqr;
                    nearest = s;
                }
            }

            if (old != nearest) {
                if (old != null) SetMarker(old, false);
                if (nearest != null) SetMarker(nearest, true);
            }
        }

        void Interact(Spot s) {
            switch (s.kind) {
                case SpotKind.Loot:
                    Loot(s);
                    break;
                case SpotKind.SchoolEntrance:
                    if (!HasItem("Ficklampa") || !HasItem("Säkring")) {
                        Flash("Entrén är låst. Du behöver Ficklampa och Säkring.", 3.2f);
                    } else {
                        StartCoroutine(EntranceRoutine(s));
                    }
                    break;
                case SpotKind.InteriorExit:
                    Teleport(s.teleportTarget);
                    insideSchool = false;
                    Flash("Du går ut på skolgården.", 2.4f);
                    break;
                case SpotKind.Door:
                    ToggleDoor(s, true);
                    break;
                case SpotKind.LockedDoor:
                    if (CanUnlock(s)) ToggleDoor(s, true);
                    else Flash(s.displayName + " är låst. Du behöver " + s.requiredItem + ".", 3.0f);
                    break;
                case SpotKind.Cabinet:
                    ToggleContainer(s, true);
                    break;
                case SpotKind.LockedCabinet:
                    if (CanUnlock(s)) ToggleContainer(s, true);
                    else Flash(s.displayName + " är låst. Du behöver " + s.requiredItem + ".", 3.0f);
                    break;
                case SpotKind.SportsHallDoor:
                    if (CanUnlock(s)) {
                        ToggleDoor(s, true);
                        sportsHallReached = true;
                        Flash("Huvudnyckeln passar. Sporthallen är öppen.", 3.5f);
                    } else {
                        Flash("Dörren mot sporthallen är låst. Hitta Huvudnyckeln.", 3.2f);
                    }
                    break;
            }
        }

        IEnumerator EntranceRoutine(Spot s) {
            busy = true;
            yield return AnimateOpen(s, true);
            yield return new WaitForSeconds(.10f);
            Teleport(s.teleportTarget);
            insideSchool = true;
            Flash("ROSVIKS SKOLA — strömmen är borta. Sök rummen.", 3.5f);
            busy = false;
        }

        void Loot(Spot s) {
            if (searched.Contains(s.spot)) return;
            searched.Add(s.spot);
            AddItem(s.itemName);
            Flash(s.displayName + ": " + (string.IsNullOrWhiteSpace(s.itemName) ? "Tomt" : s.itemName), 2.7f);
            HideSearchVisual(s.spot);
            SetMarker(s, false);
        }

        void ToggleDoor(Spot s, bool message) {
            bool next = !IsOpen(s);
            StartCoroutine(DoorRoutine(s, next, message));
        }

        IEnumerator DoorRoutine(Spot s, bool open, bool message) {
            busy = true;
            yield return AnimateOpen(s, open);
            openState[s.spot] = open;
            if (message) Flash(open ? s.displayName + " öppnas." : s.displayName + " stängs.", 1.8f);
            busy = false;
        }

        void ToggleContainer(Spot s, bool message) {
            bool next = !IsOpen(s);
            StartCoroutine(ContainerRoutine(s, next, message));
        }

        IEnumerator ContainerRoutine(Spot s, bool open, bool message) {
            busy = true;
            yield return AnimateOpen(s, open);
            openState[s.spot] = open;

            if (open && !searched.Contains(s.spot)) {
                searched.Add(s.spot);
                AddItem(s.itemName);
                if (!string.IsNullOrWhiteSpace(s.itemName))
                    Flash(s.displayName + ": du hittar " + s.itemName + ".", 2.8f);
                else if (message)
                    Flash(s.displayName + " är tomt.", 2.0f);
            } else if (message) {
                Flash(open ? s.displayName + " öppnas." : s.displayName + " stängs.", 1.6f);
            }
            busy = false;
        }

        IEnumerator AnimateOpen(Spot s, bool open) {
            Quaternion a0 = s.movingPart ? s.movingPart.localRotation : Quaternion.identity;
            Quaternion a1 = s.movingPart ? Quaternion.Euler(open ? s.openEuler : s.closedEuler) : Quaternion.identity;
            Quaternion b0 = s.movingPart2 ? s.movingPart2.localRotation : Quaternion.identity;
            Quaternion b1 = s.movingPart2 ? Quaternion.Euler(open ? s.openEuler2 : s.closedEuler2) : Quaternion.identity;

            float t = 0f;
            float duration = Mathf.Max(.05f, animationTime);
            while (t < 1f) {
                t += Time.deltaTime / duration;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                if (s.movingPart) s.movingPart.localRotation = Quaternion.Slerp(a0, a1, e);
                if (s.movingPart2) s.movingPart2.localRotation = Quaternion.Slerp(b0, b1, e);
                yield return null;
            }
            if (s.movingPart) s.movingPart.localRotation = a1;
            if (s.movingPart2) s.movingPart2.localRotation = b1;
        }

        bool CanUnlock(Spot s) {
            return string.IsNullOrWhiteSpace(s.requiredItem) || HasItem(s.requiredItem);
        }

        bool IsOpen(Spot s) {
            return s != null && s.spot && openState.TryGetValue(s.spot, out bool open) && open;
        }

        void AddItem(string item) {
            if (!string.IsNullOrWhiteSpace(item) && !inventory.Contains(item)) inventory.Add(item);
        }

        bool HasItem(string item) {
            return string.IsNullOrWhiteSpace(item) || inventory.Contains(item);
        }

        void Teleport(Vector3 target) {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.position = target;
            if (cc) cc.enabled = true;
        }

        static void HideSearchVisual(GameObject go) {
            if (!go) return;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("loot marker") || n.Contains("search marker") || n.Contains("pickup"))
                    t.gameObject.SetActive(false);
            }
        }

        static void SetMarker(Spot s, bool active) {
            if (s != null && s.marker) s.marker.SetActive(active);
        }

        void Flash(string text, float seconds) {
            flash = text;
            flashUntil = Time.time + seconds;
        }

        string Objective() {
            if (!HasItem("Ficklampa") || !HasItem("Säkring")) return "MÅL  Hitta ficklampa och säkring";
            if (!insideSchool) return "MÅL  Gå in i Rosviks skola";
            if (!HasItem("Nyckelknippa")) return "MÅL  Sök klassrummen";
            if (!HasItem("Huvudnyckel")) return "MÅL  Lås upp personalrummet";
            if (!sportsHallReached) return "MÅL  Ta dig till sporthallen";
            return "MÅL  Utforska sporthallen";
        }

        void OnGUI() {
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(.95f,.91f,.79f);
            GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            text.normal.textColor = new Color(.92f,.92f,.88f);
            GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            prompt.normal.textColor = Color.white;

            float h = 105f + inventory.Count * 20f;
            GUI.Box(new Rect(16,16,355,h), "");
            GUI.Label(new Rect(30,24,325,24), "ROSVIK // STRÖMAVBROTT", title);
            GUI.Label(new Rect(30,51,325,22), Objective(), text);
            GUI.Label(new Rect(30,74,325,22), "WASD rörelse  •  Shift spring  •  E interagera", text);
            for (int i=0;i<inventory.Count;i++) GUI.Label(new Rect(30,100+i*20,315,20), "• " + inventory[i], text);

            if (nearest != null && !busy) {
                string action = Prompt(nearest);
                GUI.Box(new Rect(Screen.width*.5f-225f,Screen.height-88f,450f,46f),"");
                GUI.Label(new Rect(Screen.width*.5f-220f,Screen.height-86f,440f,42f),action,prompt);
            }
            if (Time.time < flashUntil) {
                GUI.Box(new Rect(Screen.width*.5f-285f,18f,570f,46f),"");
                GUI.Label(new Rect(Screen.width*.5f-278f,20f,556f,42f),flash,prompt);
            }
        }

        string Prompt(Spot s) {
            switch (s.kind) {
                case SpotKind.Loot: return "E  Sök " + s.displayName;
                case SpotKind.SchoolEntrance: return "E  Öppna skolentrén";
                case SpotKind.InteriorExit: return "E  Gå ut";
                case SpotKind.Cabinet:
                case SpotKind.LockedCabinet: return "E  " + (IsOpen(s) ? "Stäng " : "Öppna ") + s.displayName;
                default: return "E  " + (IsOpen(s) ? "Stäng " : "Öppna ") + s.displayName;
            }
        }
    }
}
