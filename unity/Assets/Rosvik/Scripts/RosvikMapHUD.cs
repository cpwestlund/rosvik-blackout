using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    public class RosvikMapHUD : MonoBehaviour {
        [Serializable]
        public struct Poi {
            public string name;
            public Vector3 worldPosition;
        }

        public Camera mapCamera;
        public List<Poi> points = new List<Poi>();
        public bool showLabels = true;
        public float maxLabelDistance = 170f;

        GUIStyle labelStyle;
        GUIStyle titleStyle;

        void EnsureStyles() {
            if (labelStyle != null) return;
            labelStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.93f,.91f,.84f,.94f) }
            };
            titleStyle = new GUIStyle(labelStyle) {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        void OnGUI() {
            if (!showLabels) return;
            if (!mapCamera) mapCamera = Camera.main;
            if (!mapCamera) return;
            EnsureStyles();

            Vector3 center = mapCamera.transform.position;
            center.y = 0f;

            for (int i = 0; i < points.Count; i++) {
                Poi p = points[i];
                Vector3 flat = p.worldPosition; flat.y = 0f;
                if (Vector3.Distance(center, flat) > maxLabelDistance) continue;

                Vector3 sp = mapCamera.WorldToScreenPoint(p.worldPosition + Vector3.up * .35f);
                if (sp.z < 0f) continue;
                float y = Screen.height - sp.y;
                Rect shadow = new Rect(sp.x - 121f, y - 12f + 1f, 242f, 24f);
                Rect rect = new Rect(sp.x - 120f, y - 12f, 240f, 24f);

                Color old = GUI.color;
                GUI.color = new Color(0f,0f,0f,.45f);
                GUI.Label(shadow, p.name, titleStyle);
                GUI.color = old;
                GUI.Label(rect, p.name, titleStyle);
            }

            Rect hint = new Rect(16f, Screen.height - 42f, 560f, 26f);
            GUI.Label(hint, "WASD flyttar • Shift springer • E interagerar • mushjul zoomar", labelStyle);
        }
    }
}
