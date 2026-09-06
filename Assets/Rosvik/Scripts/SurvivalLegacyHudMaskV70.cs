using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(900)]
    public sealed class SurvivalLegacyHudMaskV70 : MonoBehaviour {
        SurvivalPresentationV70 presentation;
        bool inventoryOpen;
        GUIStyle hint;

        void Awake() {
            presentation = GetComponent<SurvivalPresentationV70>();
            if (!presentation) presentation = FindFirstObjectByType<SurvivalPresentationV70>();
        }

        void Update() {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
                inventoryOpen = !inventoryOpen;
        }

        void OnGUI() {
            if (!presentation) return;

            // Legacy V57/V69 IMGUI renders at depth 0. This layer sits above it,
            // while the polished V70 presentation (depth -1000) renders above us.
            GUI.depth = -500;
            Color old = GUI.color;

            if (inventoryOpen) {
                GUI.color = new Color(.025f, .032f, .029f, 1f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = old;
                return;
            }

            Color panel = new Color(.055f, .07f, .065f, 1f);
            GUI.color = panel;

            // Old CoziPlayer title/instructions + old V69 survival panel.
            GUI.DrawTexture(new Rect(12, 12, 350, 104), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(14, 104, 232, 178), Texture2D.whiteTexture);

            // Old flashlight + old V69 weather panel. This is fully underneath V70's right card.
            GUI.DrawTexture(new Rect(Screen.width - 258, 12, 246, 164), Texture2D.whiteTexture);
            GUI.color = old;

            // The small visible tail under the new left HUD becomes useful instead of a blank mask.
            if (hint == null) {
                hint = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                hint.normal.textColor = new Color(.78f, .78f, .70f);
            }
            GUI.Label(new Rect(28, 248, 205, 20), "E interagera  •  F ficklampa", hint);
        }
    }
}
