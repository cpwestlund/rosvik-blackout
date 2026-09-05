using UnityEngine;

namespace Rosvik.Blackout {
    /// <summary>
    /// Feeds locomotion speed into the imported humanoid Animator. If the animation
    /// controller cannot be loaded for any reason, the visual still gets a restrained
    /// procedural step/bob so the player never falls back to a completely static pawn.
    /// </summary>
    public sealed class RosvikCharacterDriver : MonoBehaviour {
        public Animator animator;
        public Transform visualRoot;
        public float fullSpeed = 6f;
        public float animationDamp = .10f;
        public float fallbackBob = .025f;
        public float fallbackLean = 1.6f;

        Vector3 lastPosition;
        Vector3 baseLocalPosition;
        Quaternion baseLocalRotation;
        bool initialized;
        float phase;

        void OnEnable() {
            lastPosition = transform.position;
            if (visualRoot) {
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }
            initialized = true;
        }

        void LateUpdate() {
            if (!initialized) OnEnable();
            if (!visualRoot && animator) visualRoot = animator.transform;

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            float speed = Time.deltaTime > .0001f ? delta.magnitude / Time.deltaTime : 0f;
            lastPosition = transform.position;
            float normalized = Mathf.Clamp01(speed / Mathf.Max(.1f, fullSpeed));

            bool animated = animator && animator.runtimeAnimatorController;
            if (animated) {
                animator.SetFloat("Speed", normalized, animationDamp, Time.deltaTime);
                animator.speed = normalized > .025f ? Mathf.Lerp(.78f, 1.12f, normalized) : 1f;
            }

            if (!visualRoot) return;
            if (animated) {
                // Keep import/root offsets stable. Humanoid clips handle the visible motion.
                visualRoot.localPosition = baseLocalPosition;
                visualRoot.localRotation = baseLocalRotation;
                return;
            }

            if (normalized > .02f) phase += Time.deltaTime * Mathf.Lerp(7f, 11f, normalized);
            float bob = normalized > .02f ? Mathf.Abs(Mathf.Sin(phase)) * fallbackBob : 0f;
            float lean = normalized > .02f ? Mathf.Sin(phase * .5f) * fallbackLean * normalized : 0f;
            visualRoot.localPosition = baseLocalPosition + Vector3.up * bob;
            visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(lean, 0f, 0f);
        }
    }
}
