using System.Collections;
using System.Collections.Generic;
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

        bool opened;
        bool consumed;
        Coroutine anim;
        MaterialPropertyBlock block;
        Color originalColor = Color.white;
        int colorProperty = -1;

        public bool IsAvailable => !consumed && gameObject.activeInHierarchy;
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
                if (!string.IsNullOrWhiteSpace(itemName)) player.AddItem(itemName);
                player.ShowToast(string.IsNullOrWhiteSpace(itemName) ? displayName + " är tom" : "Hittade: " + itemName, 2.2f);
                SetFocused(false);
                foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
                return;
            }

            bool next = !opened;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate(next, player));
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
                if (!string.IsNullOrWhiteSpace(itemName)) {
                    player.AddItem(itemName);
                    player.ShowToast(displayName + " — " + itemName, 2.3f);
                }
            }
        }
    }

    public sealed class CoziPlayerV57 : MonoBehaviour {
        public float walkSpeed = 3.8f;
        public float sprintSpeed = 5.8f;
        public float turnSharpness = 14f;
        public float interactionScanRadius = 2.4f;

        CharacterController controller;
        Camera cam;
        CozyInteractableV57 focused;
        readonly List<string> inventory = new List<string>();
        string toast = "";
        float toastUntil;
        float nextScan;

        void Awake() {
            controller = GetComponent<CharacterController>();
            cam = Camera.main;
        }

        void Update() {
            if (!cam) cam = Camera.main;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            Vector2 raw = Vector2.zero;
            if (kb.wKey.isPressed) raw.y += 1f;
            if (kb.sKey.isPressed) raw.y -= 1f;
            if (kb.dKey.isPressed) raw.x += 1f;
            if (kb.aKey.isPressed) raw.x -= 1f;
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

            if (Time.time >= nextScan) {
                nextScan = Time.time + .08f;
                Scan();
            }
            if (focused && kb.eKey.wasPressedThisFrame) focused.Interact(this);
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

        public void AddItem(string item) {
            if (!string.IsNullOrWhiteSpace(item) && !inventory.Contains(item)) inventory.Add(item);
        }
        public bool HasItem(string item) => string.IsNullOrWhiteSpace(item) || inventory.Contains(item);
        public void ShowToast(string text, float seconds) { toast = text; toastUntil = Time.time + seconds; }

        void OnGUI() {
            GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            small.normal.textColor = new Color(.94f,.92f,.85f);
            GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            prompt.normal.textColor = Color.white;

            GUI.Box(new Rect(18,18,255,54), "");
            GUI.Label(new Rect(31,27,230,20), "STRÖMAVBROTT", prompt);
            GUI.Label(new Rect(31,49,230,18), "WASD  •  Shift  •  E", small);

            if (focused) {
                GUI.Box(new Rect(Screen.width*.5f-185f, Screen.height-76f, 370f, 42f), "");
                GUI.Label(new Rect(Screen.width*.5f-178f, Screen.height-74f, 356f, 38f), "E  " + focused.Prompt(this), prompt);
            }
            if (Time.time < toastUntil) {
                GUI.Box(new Rect(Screen.width*.5f-210f, 20f, 420f, 42f), "");
                GUI.Label(new Rect(Screen.width*.5f-202f, 22f, 404f, 38f), toast, prompt);
            }
        }
    }
}
