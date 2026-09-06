using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public sealed class CozyCameraV57 : MonoBehaviour {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 17.5f, -14.5f);
        public float followSharpness = 10f;
        public float lookAhead = 0.8f;
        public float minSize = 6.5f;
        public float maxSize = 11.5f;
        public float zoomSpeed = 0.8f;
        Camera cam;

        void Awake() {
            cam = GetComponent<Camera>();
            if (cam) cam.orthographic = true;
        }

        void LateUpdate() {
            if (!target || !cam) return;
            Vector3 desiredTarget = target.position + target.forward * lookAhead;
            Vector3 desired = desiredTarget + offset;
            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
            transform.rotation = Quaternion.LookRotation((desiredTarget - transform.position).normalized, Vector3.up);

            Mouse mouse = Mouse.current;
            if (mouse != null) {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f)
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Mathf.Sign(scroll) * zoomSpeed, minSize, maxSize);
            }
        }
    }

    public sealed class CozyInteractableV57 : MonoBehaviour {
        public enum Kind { Door, Cabinet, Loot }
        public Kind kind;
        public string displayName = "objektet";
        public string itemName = "";
        public string[] extraItems = Array.Empty<string>();
        public int[] extraCounts = Array.Empty<int>();
        public string requiredItem = "";
        public float radius = 1.9f;
        public Transform movingPart;
        public Transform movingPart2;
        public Vector3 closedEuler;
        public Vector3 openEuler;
        public Vector3 closedEuler2;
        public Vector3 openEuler2;
        public Transform revealOnOpen;
        public Renderer highlightRenderer;
        public Color highlightColor = new Color(1f, .73f, .28f, 1f);
        public float animationTime = .22f;
        public string objectiveAfterUse = "";

        bool opened;
        bool consumed;
        Coroutine anim;
        MaterialPropertyBlock block;
        Color originalColor = Color.white;
        int colorProperty = -1;

        public bool IsAvailable => gameObject.activeInHierarchy && (kind != Kind.Loot || !consumed);
        public bool IsOpen => opened;

        void Awake() {
            if (revealOnOpen) revealOnOpen.gameObject.SetActive(opened);
            CacheHighlight();
        }

        void CacheHighlight() {
            if (!highlightRenderer || !highlightRenderer.sharedMaterial) return;
            Material m = highlightRenderer.sharedMaterial;
            if (m.HasProperty("_BaseColor")) { colorProperty = Shader.PropertyToID("_BaseColor"); originalColor = m.GetColor("_BaseColor"); }
            else if (m.HasProperty("_Color")) { colorProperty = Shader.PropertyToID("_Color"); originalColor = m.GetColor("_Color"); }
            block = new MaterialPropertyBlock();
        }

        public void SetFocused(bool focused) {
            if (!highlightRenderer || colorProperty < 0 || block == null) return;
            highlightRenderer.GetPropertyBlock(block);
            block.SetColor(colorProperty, focused ? Color.Lerp(originalColor, highlightColor, .45f) : originalColor);
            highlightRenderer.SetPropertyBlock(block);
        }

        public string Prompt(CoziPlayerV57 player) {
            if (kind == Kind.Door) return (opened ? "Stäng " : "Öppna ") + displayName;
            if (kind == Kind.Cabinet) return (opened ? "Stäng " : "Öppna ") + displayName;
            return "Sök " + displayName;
        }

        public void Interact(CoziPlayerV57 player) {
            if (!IsAvailable || !player) return;
            if (!string.IsNullOrWhiteSpace(requiredItem) && !player.HasItem(requiredItem)) {
                player.ShowToast(displayName + " är låst — behöver " + requiredItem, 2.4f);
                return;
            }

            if (kind == Kind.Loot) {
                consumed = true;
                string found = GiveLoot(player);
                player.ShowToast(string.IsNullOrWhiteSpace(found) ? displayName + " är tom" : "Hittade: " + found, 2.5f);
                SetFocused(false);
                foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
                if (!string.IsNullOrWhiteSpace(objectiveAfterUse)) player.SetObjective(objectiveAfterUse);
                return;
            }

            bool next = !opened;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate(next, player));
        }

        string GiveLoot(CoziPlayerV57 player) {
            List<string> found = new List<string>();
            if (!string.IsNullOrWhiteSpace(itemName)) {
                player.AddItem(itemName, 1);
                found.Add(itemName);
            }
            if (extraItems != null) {
                for (int i = 0; i < extraItems.Length; i++) {
                    string item = extraItems[i];
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    int count = 1;
                    if (extraCounts != null && i < extraCounts.Length) count = Mathf.Max(1, extraCounts[i]);
                    player.AddItem(item, count);
                    found.Add(count > 1 ? item + " x" + count : item);
                }
            }
            return string.Join(", ", found);
        }

        IEnumerator Animate(bool targetOpen, CoziPlayerV57 player) {
            Quaternion a0 = movingPart ? movingPart.localRotation : Quaternion.identity;
            Quaternion a1 = movingPart ? Quaternion.Euler(targetOpen ? openEuler : closedEuler) : Quaternion.identity;
            Quaternion b0 = movingPart2 ? movingPart2.localRotation : Quaternion.identity;
            Quaternion b1 = movingPart2 ? Quaternion.Euler(targetOpen ? openEuler2 : closedEuler2) : Quaternion.identity;
            float t = 0f;
            while (t < 1f) {
                t += Time.deltaTime / Mathf.Max(.06f, animationTime);
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                if (movingPart) movingPart.localRotation = Quaternion.Slerp(a0, a1, e);
                if (movingPart2) movingPart2.localRotation = Quaternion.Slerp(b0, b1, e);
                yield return null;
            }
            if (movingPart) movingPart.localRotation = a1;
            if (movingPart2) movingPart2.localRotation = b1;
            opened = targetOpen;
            if (revealOnOpen) revealOnOpen.gameObject.SetActive(opened);

            if (opened && kind == Kind.Cabinet && !consumed) {
                consumed = true;
                string found = GiveLoot(player);
                if (!string.IsNullOrWhiteSpace(found)) player.ShowToast(displayName + " — " + found, 2.8f);
                else player.ShowToast(displayName + " är tom", 1.8f);
                if (!string.IsNullOrWhiteSpace(objectiveAfterUse)) player.SetObjective(objectiveAfterUse);
            }
        }
    }

    public sealed class CoziPlayerV57 : MonoBehaviour {
        public float walkSpeed = 3.8f;
        public float sprintSpeed = 5.8f;
        public float turnSharpness = 14f;
        public float interactionScanRadius = 2.4f;
        public Light flashlight;
        public float flashlightBattery = 0f;
        public float flashlightDrainPerSecond = .7f;
        public string objective = "Sök igenom skolan efter användbara saker";
        public bool suppressLegacyGui = false;

        CharacterController controller;
        Camera cam;
        CozyInteractableV57 focused;
        readonly Dictionary<string,int> inventory = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        string toast = "";
        float toastUntil;
        float nextScan;
        bool inventoryOpen;
        bool flashlightOn;

        public bool FlashlightOn => flashlightOn;

        void Awake() {
            controller = GetComponent<CharacterController>();
            cam = Camera.main;
            if (flashlight) flashlight.enabled = false;
        }

        void Update() {
            if (!cam) cam = Camera.main;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) inventoryOpen = !inventoryOpen;
            if (kb.fKey.wasPressedThisFrame) ToggleFlashlight();

            Vector2 raw = Vector2.zero;
            if (!inventoryOpen) {
                if (kb.wKey.isPressed) raw.y += 1f;
                if (kb.sKey.isPressed) raw.y -= 1f;
                if (kb.dKey.isPressed) raw.x += 1f;
                if (kb.aKey.isPressed) raw.x -= 1f;
            }
            raw = Vector2.ClampMagnitude(raw, 1f);

            Vector3 forward = cam ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam ? cam.transform.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();
            Vector3 move = forward * raw.y + right * raw.x;
            float speed = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) ? sprintSpeed : walkSpeed;

            if (controller) {
                Vector3 delta = move * speed;
                delta.y = -1.5f;
                controller.Move(delta * Time.deltaTime);
            } else transform.position += move * speed * Time.deltaTime;

            if (move.sqrMagnitude > .01f) {
                Quaternion targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSharpness * Time.deltaTime));
            }

            if (flashlightOn) {
                flashlightBattery = Mathf.Max(0f, flashlightBattery - flashlightDrainPerSecond * Time.deltaTime);
                if (flashlightBattery <= 0f) {
                    flashlightOn = false;
                    if (flashlight) flashlight.enabled = false;
                    ShowToast("Ficklampans batteri tog slut", 2.4f);
                }
            }

            if (Time.time >= nextScan) {
                nextScan = Time.time + .08f;
                Scan();
            }
            if (!inventoryOpen && focused && kb.eKey.wasPressedThisFrame) focused.Interact(this);
        }

        void ToggleFlashlight() {
            if (!HasItem("Ficklampa")) { ShowToast("Du har ingen ficklampa", 1.8f); return; }
            if (flashlightBattery <= 0f) {
                if (CountItem("Batterier") > 0) {
                    ConsumeItem("Batterier", 1);
                    flashlightBattery = 100f;
                    ShowToast("Bytte batterier i ficklampan", 2f);
                } else {
                    ShowToast("Ficklampan behöver batterier", 2f);
                    return;
                }
            }
            flashlightOn = !flashlightOn;
            if (flashlight) flashlight.enabled = flashlightOn;
        }

        void Scan() {
            CozyInteractableV57 best = null;
            float bestSq = float.MaxValue;
            CozyInteractableV57[] all = FindObjectsByType<CozyInteractableV57>(FindObjectsSortMode.None);
            Vector3 here = transform.position;
            foreach (CozyInteractableV57 x in all) {
                if (!x || !x.IsAvailable) continue;
                Vector3 d = x.transform.position - here; d.y = 0f;
                float r = Mathf.Max(.6f, x.radius);
                float sq = d.sqrMagnitude;
                if (sq <= r*r && sq < bestSq) { best = x; bestSq = sq; }
            }
            if (focused == best) return;
            if (focused) focused.SetFocused(false);
            focused = best;
            if (focused) focused.SetFocused(true);
        }

        public void AddItem(string item) { AddItem(item, 1); }
        public void AddItem(string item, int count) {
            if (string.IsNullOrWhiteSpace(item) || count <= 0) return;
            if (!inventory.ContainsKey(item)) inventory[item] = 0;
            inventory[item] += count;
            if (item.Equals("Ficklampa", StringComparison.OrdinalIgnoreCase) && flashlightBattery <= 0f) flashlightBattery = 45f;
        }
        public int CountItem(string item) {
            if (string.IsNullOrWhiteSpace(item)) return 0;
            return inventory.TryGetValue(item, out int n) ? n : 0;
        }
        public bool HasItem(string item) => string.IsNullOrWhiteSpace(item) || CountItem(item) > 0;
        public bool ConsumeItem(string item, int count) {
            if (CountItem(item) < count) return false;
            inventory[item] -= count;
            if (inventory[item] <= 0) inventory.Remove(item);
            return true;
        }
        public IReadOnlyDictionary<string,int> Inventory => inventory;
        public void SetObjective(string text) { if (!string.IsNullOrWhiteSpace(text)) objective = text; }
        public void ShowToast(string text, float seconds) { toast = text; toastUntil = Time.time + seconds; }

        void OnGUI() {
            if (suppressLegacyGui) return;
            GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            small.normal.textColor = new Color(.94f,.92f,.85f);
            GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            prompt.normal.textColor = Color.white;
            GUIStyle title = new GUIStyle(prompt) { fontSize = 19 };
            GUIStyle item = new GUIStyle(small) { fontSize = 15 };

            GUI.Box(new Rect(18,18,330,86), "");
            GUI.Label(new Rect(31,27,300,22), "STRÖMAVBROTT", title);
            GUI.Label(new Rect(31,51,300,18), "WASD • Shift • E • I inventarie • F ficklampa", small);
            GUI.Label(new Rect(31,72,300,22), "Mål: " + objective, small);

            if (flashlight && HasItem("Ficklampa")) {
                GUI.Box(new Rect(Screen.width - 190, 18, 172, 48), "");
                GUI.Label(new Rect(Screen.width - 180, 27, 150, 18), flashlightOn ? "Ficklampa: PÅ" : "Ficklampa: AV", small);
                GUI.Label(new Rect(Screen.width - 180, 45, 150, 18), "Batteri " + Mathf.CeilToInt(flashlightBattery) + "%", small);
            }

            if (inventoryOpen) {
                float w = 430f, h = 390f;
                Rect panel = new Rect(Screen.width*.5f-w*.5f, Screen.height*.5f-h*.5f, w, h);
                GUI.Box(panel, "");
                GUI.Label(new Rect(panel.x+20,panel.y+16,w-40,28), "INVENTARIE", title);
                GUI.Label(new Rect(panel.x+20,panel.y+46,w-40,20), "I / Tab stänger", small);
                int i = 0;
                foreach (var kv in inventory.OrderBy(x => x.Key)) {
                    int col = i % 2;
                    int row = i / 2;
                    Rect slot = new Rect(panel.x+20+col*195, panel.y+82+row*48, 180, 38);
                    GUI.Box(slot, "");
                    GUI.Label(new Rect(slot.x+10,slot.y+9,160,20), kv.Key + (kv.Value > 1 ? "  x" + kv.Value : ""), item);
                    i++;
                    if (row >= 5) break;
                }
                if (inventory.Count == 0) GUI.Label(new Rect(panel.x+20,panel.y+90,w-40,24), "Tomt.", item);
            }

            if (!inventoryOpen && focused) {
                GUI.Box(new Rect(Screen.width*.5f-205f, Screen.height-76f, 410f, 42f), "");
                GUI.Label(new Rect(Screen.width*.5f-198f, Screen.height-74f, 396f, 38f), "E  " + focused.Prompt(this), prompt);
            }
            if (Time.time < toastUntil) {
                GUI.Box(new Rect(Screen.width*.5f-245f, 20f, 490f, 42f), "");
                GUI.Label(new Rect(Screen.width*.5f-237f, 22f, 474f, 38f), toast, prompt);
            }
        }
    }
}
