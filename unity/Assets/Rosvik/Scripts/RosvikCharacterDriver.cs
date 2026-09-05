using UnityEngine;

namespace Rosvik.Blackout {
    /// <summary>
    /// Feeds locomotion speed into the imported humanoid Animator. Some third-party
    /// animation imports can still leave a valid humanoid standing in bind/T-pose.
    /// In that case we pose the arms after Animator evaluation using the actual humanoid
    /// bone positions, so the player always has a natural silhouette and simple arm swing.
    /// </summary>
    public sealed class RosvikCharacterDriver : MonoBehaviour {
        public Animator animator;
        public Transform visualRoot;
        public float fullSpeed = 6f;
        public float animationDamp = .10f;
        public float fallbackBob = .018f;
        public float fallbackLean = 1.2f;

        Vector3 lastPosition;
        Vector3 baseLocalPosition;
        Quaternion baseLocalRotation;
        bool initialized;
        float phase;

        Transform leftUpperArm, leftHand, rightUpperArm, rightHand;
        bool bonesCached;

        void OnEnable() {
            lastPosition = transform.position;
            if (visualRoot) {
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }
            CacheBones();
            initialized = true;
        }

        void CacheBones() {
            bonesCached = true;
            if (!animator || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        void LateUpdate() {
            if (!initialized) OnEnable();
            if (!visualRoot && animator) visualRoot = animator.transform;
            if (!bonesCached) CacheBones();

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            float speed = Time.deltaTime > .0001f ? delta.magnitude / Time.deltaTime : 0f;
            lastPosition = transform.position;
            float normalized = Mathf.Clamp01(speed / Mathf.Max(.1f, fullSpeed));

            bool hasController = animator && animator.runtimeAnimatorController;
            if (hasController) {
                animator.SetFloat("Speed", normalized, animationDamp, Time.deltaTime);
                animator.speed = normalized > .025f ? Mathf.Lerp(.82f, 1.10f, normalized) : 1f;
            }

            if (!visualRoot) return;
            visualRoot.localPosition = baseLocalPosition;
            visualRoot.localRotation = baseLocalRotation;

            if (normalized > .02f)
                phase += Time.deltaTime * Mathf.Lerp(6.5f, 10f, normalized);

            bool humanoidFallback = HasHumanoidArms() && (!hasController || LooksLikeTPose());
            if (humanoidFallback) {
                PoseArm(leftUpperArm, leftHand, -1f, normalized);
                PoseArm(rightUpperArm, rightHand, 1f, normalized);

                float bob = normalized > .02f ? Mathf.Abs(Mathf.Sin(phase * 2f)) * fallbackBob : 0f;
                visualRoot.localPosition = baseLocalPosition + Vector3.up * bob;
                return;
            }

            if (hasController) return;

            // Last-resort fallback for a non-humanoid model.
            float simpleBob = normalized > .02f ? Mathf.Abs(Mathf.Sin(phase)) * fallbackBob : 0f;
            float lean = normalized > .02f ? Mathf.Sin(phase * .5f) * fallbackLean * normalized : 0f;
            visualRoot.localPosition = baseLocalPosition + Vector3.up * simpleBob;
            visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(lean, 0f, 0f);
        }

        bool HasHumanoidArms() {
            return leftUpperArm && leftHand && rightUpperArm && rightHand;
        }

        bool LooksLikeTPose() {
            if (!HasHumanoidArms()) return false;

            Vector3 l = leftHand.position - leftUpperArm.position;
            Vector3 r = rightHand.position - rightUpperArm.position;
            float lHorizontal = new Vector2(l.x, l.z).magnitude;
            float rHorizontal = new Vector2(r.x, r.z).magnitude;

            // Bind/T-pose has hands almost level with shoulders and far out to the sides.
            return Mathf.Abs(l.y) < .28f && Mathf.Abs(r.y) < .28f &&
                   lHorizontal > .32f && rHorizontal > .32f;
        }

        void PoseArm(Transform upperArm, Transform hand, float sideSign, float normalized) {
            if (!upperArm || !hand) return;

            Vector3 current = hand.position - upperArm.position;
            if (current.sqrMagnitude < .0001f) return;

            float swing = normalized > .02f ? Mathf.Sin(phase + (sideSign > 0f ? Mathf.PI : 0f)) * .24f * normalized : 0f;
            Vector3 desired = (Vector3.down * .96f
                              + transform.right * sideSign * .13f
                              + transform.forward * swing).normalized;

            Quaternion correction = Quaternion.FromToRotation(current.normalized, desired);
            upperArm.rotation = correction * upperArm.rotation;
        }
    }
}
