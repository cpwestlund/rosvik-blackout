using UnityEngine;

namespace Rosvik.Blackout {
    /// <summary>
    /// Drives the imported humanoid from actual world-space movement and adds a small
    /// procedural layer (lean, breathing and interaction reach) after Animator evaluation.
    /// The procedural layer is intentionally subtle when real clips are available, while
    /// still keeping the old bind/T-pose safety fallback for broken imports.
    /// </summary>
    public sealed class RosvikCharacterDriver : MonoBehaviour {
        public Animator animator;
        public Transform visualRoot;
        public float fullSpeed = 5.6f;
        public float animationDamp = .10f;

        [Header("Motion polish")]
        public float forwardLean = 3.2f;
        public float turnLean = 4.0f;
        public float idleBreath = .006f;
        public float movingBob = .008f;
        public float fallbackBob = .018f;
        public float fallbackLean = 1.2f;

        Vector3 lastPosition;
        Vector3 lastForward;
        Vector3 baseLocalPosition;
        Quaternion baseLocalRotation;
        bool initialized;
        float phase;
        float smoothedSpeed;
        float interactTimer;
        float interactDuration = .45f;

        Transform leftUpperArm, leftHand, rightUpperArm, rightHand;
        bool bonesCached;

        public float CurrentSpeed => smoothedSpeed;
        public bool IsMoving => smoothedSpeed > .12f;

        void OnEnable() {
            lastPosition = transform.position;
            lastForward = transform.forward;
            if (visualRoot) {
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }
            CacheBones();
            initialized = true;
        }

        public void PulseInteract(float duration = .45f) {
            interactDuration = Mathf.Max(.18f, duration);
            interactTimer = interactDuration;
        }

        void CacheBones() {
            bonesCached = true;
            leftUpperArm = leftHand = rightUpperArm = rightHand = null;
            if (!animator || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        void LateUpdate() {
            if (!initialized) OnEnable();
            if (!visualRoot && animator) {
                visualRoot = animator.transform;
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }
            if (!bonesCached) CacheBones();

            float dt = Mathf.Max(.0001f, Time.deltaTime);
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            float rawSpeed = delta.magnitude / dt;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, 1f - Mathf.Exp(-13f * dt));
            lastPosition = transform.position;

            float normalized = Mathf.Clamp01(smoothedSpeed / Mathf.Max(.1f, fullSpeed));
            float turnRate = Vector3.SignedAngle(lastForward, transform.forward, Vector3.up) / dt;
            lastForward = transform.forward;
            float normalizedTurn = Mathf.Clamp(turnRate / 220f, -1f, 1f);

            bool hasController = animator && animator.runtimeAnimatorController;
            if (hasController) {
                animator.SetFloat("Speed", normalized, animationDamp, dt);
                // State speeds are authored in the V31 controller. Keeping animator.speed at
                // one avoids the old slow-motion / skating feel at walking pace.
                animator.speed = 1f;
            }

            if (!visualRoot) return;
            visualRoot.localPosition = baseLocalPosition;
            visualRoot.localRotation = baseLocalRotation;

            if (normalized > .02f)
                phase += dt * Mathf.Lerp(6.0f, 10.5f, normalized);
            else
                phase += dt * 1.35f;

            bool humanoidFallback = HasHumanoidArms() && (!hasController || LooksLikeTPose());
            if (humanoidFallback) {
                PoseArm(leftUpperArm, leftHand, -1f, normalized);
                PoseArm(rightUpperArm, rightHand, 1f, normalized);

                float bob = normalized > .02f ? Mathf.Abs(Mathf.Sin(phase * 2f)) * fallbackBob : 0f;
                float lean = normalized > .02f ? Mathf.Sin(phase * .5f) * fallbackLean * normalized : 0f;
                visualRoot.localPosition = baseLocalPosition + Vector3.up * bob;
                visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(lean, 0f, -normalizedTurn * turnLean);
            } else {
                // Subtle secondary motion on top of the real animation clips. This is small
                // enough not to fight the rig, but removes the rigid "FBX sliding on rails" read.
                float breath = normalized < .03f ? Mathf.Sin(phase) * idleBreath : 0f;
                float bob = normalized > .03f ? Mathf.Abs(Mathf.Sin(phase)) * movingBob * normalized : 0f;
                float pitch = normalized * forwardLean;
                float roll = -normalizedTurn * turnLean * Mathf.Lerp(.35f, 1f, normalized);
                visualRoot.localPosition = baseLocalPosition + Vector3.up * (breath + bob);
                visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            }

            if (interactTimer > 0f) {
                interactTimer = Mathf.Max(0f, interactTimer - dt);
                float t = 1f - interactTimer / Mathf.Max(.01f, interactDuration);
                float pulse = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                visualRoot.localPosition += Vector3.down * (.035f * pulse);
                visualRoot.localRotation *= Quaternion.Euler(9f * pulse, 0f, 0f);
                if (HasHumanoidArms()) PoseReachArm(rightUpperArm, rightHand, pulse);
            }
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
            upperArm.rotation = Quaternion.FromToRotation(current.normalized, desired) * upperArm.rotation;
        }

        void PoseReachArm(Transform upperArm, Transform hand, float amount) {
            if (!upperArm || !hand || amount <= .001f) return;
            Vector3 current = hand.position - upperArm.position;
            if (current.sqrMagnitude < .0001f) return;
            Vector3 relaxed = current.normalized;
            Vector3 reach = (transform.forward * .72f + Vector3.down * .58f + transform.right * .08f).normalized;
            Vector3 desired = Vector3.Slerp(relaxed, reach, amount * .72f).normalized;
            upperArm.rotation = Quaternion.FromToRotation(current.normalized, desired) * upperArm.rotation;
        }
    }
}
