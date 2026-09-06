using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public class TopDownGameplayV56 : MonoBehaviour {
        public enum SpotKind { Door, LockedDoor, Cabinet, LockedCabinet, Loot }

        [System.Serializable]
        public class Spot {
            public GameObject spot;
            public GameObject marker;
            public string displayName;
            public string itemName;
            public string requiredItem;
            public SpotKind kind;
            public float interactRadius = 2.2f;
            public Transform movingPart;
            public Transform movingPart2;
            public Vector3 closedEuler;
            public Vector3 openEuler;
            public Vector3 closedEuler2;
            public Vector3 openEuler2;
        }

        public List<Spot> spots = new List<Spot>();
        public float animationTime = .18f;

        readonly Dictionary<GameObject,bool> openState = new Dictionary<GameObject,bool>();
        readonly HashSet<GameObject> searched = new HashSet<GameObject>();
        readonly List<string> inventory = new List<string>();
        Spot nearest;
        bool busy;
        string flash = "";
        float flashUntil;

        void Awake() {
            openState.Clear();
            foreach (Spot s in spots) {
                if (s == null || !s.spot) continue;
                openState[s.spot] = false;
                if (s.marker) s.marker.SetActive(false);
            }
        }

        void Update() {
            FindNearest();
            Keyboard kb = Keyboard.current;
            if (!busy && nearest != null && kb != null && kb.eKey.wasPressedThisFrame)
                Interact(nearest);
        }

        void FindNearest() {
            Spot before = nearest;
            nearest = null;
            float best = float.MaxValue;
            Vector3 p = transform.position;
            foreach (Spot s in spots) {
                if (s == null || !s.spot || !s.spot.activeInHierarchy) continue;
                if (s.kind == SpotKind.Loot && searched.Contains(s.spot)) continue;
                Vector3 d = s.spot.transform.position - p; d.y = 0f;
                float r = Mathf.Max(1.2f, s.interactRadius);
                float q = d.sqrMagnitude;
                if (q <= r*r && q < best) { best = q; nearest = s; }
            }
            if (before != nearest) {
                if (before != null && before.marker) before.marker.SetActive(false);
                if (nearest != null && nearest.marker) nearest.marker.SetActive(true);
            }
        }

        void Interact(Spot s) {
            if (s.kind == SpotKind.Loot) { TakeLoot(s); return; }
            if ((s.kind == SpotKind.LockedDoor || s.kind == SpotKind.LockedCabinet) && !HasItem(s.requiredItem)) {
                Flash(s.displayName + " är låst. Kräver " + s.requiredItem + ".", 2.5f);
                return;
            }
            bool next = !IsOpen(s);
            StartCoroutine(ToggleRoutine(s, next));
        }

        IEnumerator ToggleRoutine(Spot s, bool open) {
            busy = true;
            Quaternion a0 = s.movingPart ? s.movingPart.localRotation : Quaternion.identity;
            Quaternion a1 = s.movingPart ? Quaternion.Euler(open ? s.openEuler : s.closedEuler) : Quaternion.identity;
            Quaternion b0 = s.movingPart2 ? s.movingPart2.localRotation : Quaternion.identity;
            Quaternion b1 = s.movingPart2 ? Quaternion.Euler(open ? s.openEuler2 : s.closedEuler2) : Quaternion.identity;
            float t = 0f;
            float dur = Mathf.Max(.06f, animationTime);
            while (t < 1f) {
                t += Time.deltaTime / dur;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                if (s.movingPart) s.movingPart.localRotation = Quaternion.Slerp(a0,a1,e);
                if (s.movingPart2) s.movingPart2.localRotation = Quaternion.Slerp(b0,b1,e);
                yield return null;
            }
            if (s.movingPart) s.movingPart.localRotation = a1;
            if (s.movingPart2) s.movingPart2.localRotation = b1;
            openState[s.spot] = open;

            bool container = s.kind == SpotKind.Cabinet || s.kind == SpotKind.LockedCabinet;
            if (container && open && !searched.Contains(s.spot)) {
                searched.Add(s.spot);
                if (!string.IsNullOrWhiteSpace(s.itemName)) {
                    AddItem(s.itemName);
                    Flash(s.displayName + ": " + s.itemName, 2.2f);
                } else Flash(s.displayName + " är tomt.", 1.8f);
            } else {
                Flash(s.displayName + (open ? " öppnas." : " stängs."), 1.4f);
            }
            busy = false;
        }

        void TakeLoot(Spot s) {
            if (searched.Contains(s.spot)) return;
            searched.Add(s.spot);
            AddItem(s.itemName);
            Flash(s.displayName + ": " + (string.IsNullOrWhiteSpace(s.itemName) ? "Tomt" : s.itemName), 2.0f);
            foreach (Renderer r in s.spot.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            if (s.marker) s.marker.SetActive(false);
        }

        bool IsOpen(Spot s) {
            bool value;
            return s != null && s.spot && openState.TryGetValue(s.spot, out value) && value;
        }

        bool HasItem(string item) { return string.IsNullOrWhiteSpace(item) || inventory.Contains(item); }
        void AddItem(string item) { if (!string.IsNullOrWhiteSpace(item) && !inventory.Contains(item)) inventory.Add(item); }
        void Flash(string text, float seconds) { flash = text; flashUntil = Time.time + seconds; }

        string Prompt(Spot s) {
            if (s.kind == SpotKind.Loot) return "E  Sök " + s.displayName;
            bool open = IsOpen(s);
            if (s.kind == SpotKind.Cabinet || s.kind == SpotKind.LockedCabinet)
                return "E  " + (open ? "Stäng " : "Öppna ") + s.displayName;
            return "E  " + (open ? "Stäng " : "Öppna ") + s.displayName;
        }

        void OnGUI() {
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize=17, fontStyle=FontStyle.Bold };
            title.normal.textColor = new Color(.94f,.91f,.80f);
            GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize=13 };
            small.normal.textColor = new Color(.90f,.90f,.86f);
            GUIStyle center = new GUIStyle(GUI.skin.label) { fontSize=17, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter };
            center.normal.textColor = Color.white;

            float h = 78f + inventory.Count*19f;
            GUI.Box(new Rect(14,14,285,h),"");
            GUI.Label(new Rect(27,21,255,22),"ROSVIK // STRÖMAVBROTT",title);
            GUI.Label(new Rect(27,46,255,20),"WASD  •  Shift  •  E interagera",small);
            for(int i=0;i<inventory.Count;i++) GUI.Label(new Rect(27,69+i*19,245,19),"• "+inventory[i],small);

            if (nearest != null && !busy) {
                GUI.Box(new Rect(Screen.width*.5f-220f,Screen.height-82f,440f,42f),"");
                GUI.Label(new Rect(Screen.width*.5f-214f,Screen.height-80f,428f,38f),Prompt(nearest),center);
            }
            if (Time.time < flashUntil) {
                GUI.Box(new Rect(Screen.width*.5f-245f,16f,490f,42f),"");
                GUI.Label(new Rect(Screen.width*.5f-238f,18f,476f,38f),flash,center);
            }
        }
    }
}
