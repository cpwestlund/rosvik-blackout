using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(-2500)]
    public sealed class HouseDoorAccessV762 : MonoBehaviour {
        public CozyInteractableV57 houseDoor;
        public float interactionDistance = 2.75f;
        CoziPlayerV57 player;
        bool blockThisFrame;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            ResolveDoor();
        }

        void ResolveDoor() {
            if (houseDoor) return;
            foreach (CozyInteractableV57 x in FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (x && x.kind == CozyInteractableV57.Kind.Door && string.Equals(x.displayName, "husets ytterdörr", System.StringComparison.OrdinalIgnoreCase)) {
                    houseDoor = x;
                    break;
                }
            }
        }

        void Update() {
            if (!player) return;
            if (!houseDoor) ResolveDoor();
            if (!houseDoor || !houseDoor.gameObject.activeInHierarchy || player.externalUiBlocked) return;

            Vector3 d = houseDoor.transform.position - player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > interactionDistance * interactionDistance) return;

            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;

            // Own the E press for this frame so the old generic scanner cannot toggle the same door twice.
            player.externalUiBlocked = true;
            blockThisFrame = true;
            houseDoor.Interact(player);
        }

        void LateUpdate() {
            if (!blockThisFrame || !player) return;
            blockThisFrame = false;
            player.externalUiBlocked = false;
        }

        void OnGUI() {
            if (!player || !houseDoor || player.externalUiBlocked) return;
            Vector3 d = houseDoor.transform.position - player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > interactionDistance * interactionDistance) return;

            Rect r = new Rect(Screen.width * .5f - 190f, Screen.height - 62f, 380f, 34f);
            Color old = GUI.color;
            GUI.color = new Color(.025f, .03f, .027f, .94f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUIStyle s = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            s.normal.textColor = new Color(.90f, .89f, .80f);
            GUI.Label(r, "E   " + (houseDoor.IsOpen ? "STÄNG YTTERDÖRREN" : "ÖPPNA YTTERDÖRREN"), s);
        }
    }

    [DefaultExecutionOrder(9800)]
    public sealed class HouseInteriorVisibilityV762 : MonoBehaviour {
        public GameObject roofRoot;
        public Vector2 minXZ = new Vector2(-36.2f, 2.55f);
        public Vector2 maxXZ = new Vector2(-21.8f, 15.8f);
        CoziPlayerV57 player;
        bool lastInside;
        bool initialized;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            ResolveRoof();
            Apply(true);
        }

        void ResolveRoof() {
            if (roofRoot) return;
            roofRoot = GameObject.Find("HOUSE ROOF V76.2");
        }

        bool InsideHouse() {
            if (!player) return false;
            Vector3 p = player.transform.position;
            return p.x > minXZ.x && p.x < maxXZ.x && p.z > minXZ.y && p.z < maxXZ.y;
        }

        void LateUpdate() { Apply(false); }

        void Apply(bool force) {
            if (!roofRoot) ResolveRoof();
            if (!roofRoot || !player) return;
            bool inside = InsideHouse();
            if (!force && initialized && inside == lastInside) return;
            initialized = true;
            lastInside = inside;
            // Outside: roof hides the interior. Once the player crosses the doorway, reveal the cutaway interior.
            roofRoot.SetActive(!inside);
        }
    }
}
