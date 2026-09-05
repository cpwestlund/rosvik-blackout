using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    /// <summary>
    /// Very small whole-object sway for the stylised vegetation. It is intentionally
    /// conservative: enough to stop the world feeling frozen, not enough to look rubbery.
    /// </summary>
    public sealed class RosvikAmbientMotion : MonoBehaviour {
        public Transform sourceRoot;
        public float windStrength = 1.05f;
        public float windSpeed = .58f;
        public int maxAnimated = 90;

        readonly List<Transform> targets = new List<Transform>();
        readonly List<Quaternion> bases = new List<Quaternion>();
        readonly List<float> phases = new List<float>();

        void Start() {
            if (!sourceRoot) sourceRoot = transform.root;
            Rebuild();
        }

        public void Rebuild() {
            targets.Clear(); bases.Clear(); phases.Clear();
            if (!sourceRoot) return;

            foreach (Transform t in sourceRoot.GetComponentsInChildren<Transform>(true)) {
                if (!t || t == sourceRoot || targets.Count >= maxAnimated) continue;
                string n = t.name.ToLowerInvariant();
                bool nature = n == "tree" || n.Contains("edge tree") || n.Contains("pine") ||
                              n == "bush" || n.Contains("bush") || n.Contains("shrub");
                if (!nature) continue;
                if (t.GetComponentInChildren<Renderer>(true) == null) continue;

                targets.Add(t);
                bases.Add(t.localRotation);
                phases.Add(Mathf.Abs(t.position.x * .071f + t.position.z * .113f) % 6.28318f);
            }
        }

        void LateUpdate() {
            float time = Time.time * windSpeed;
            int count = Mathf.Min(targets.Count, Mathf.Min(bases.Count, phases.Count));
            for (int i = 0; i < count; i++) {
                Transform t = targets[i];
                if (!t) continue;
                float a = Mathf.Sin(time + phases[i]) * windStrength;
                float b = Mathf.Sin(time * .73f + phases[i] * 1.71f) * windStrength * .38f;
                t.localRotation = bases[i] * Quaternion.Euler(b, 0f, a);
            }
        }
    }
}
