using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(13000)]
    public sealed class HouseVisualRebuildV85 : MonoBehaviour {
        public GameObject exteriorShell;
        public GameObject interiorVisuals;
        public GameObject exteriorDetails;
        public Vector2 minXZ = new Vector2(-36.2f, 2.2f);
        public Vector2 maxXZ = new Vector2(-21.8f, 15.8f);
        public float enterPadding = .28f;
        public float exitPadding = .55f;

        CoziPlayerV57 player;
        bool initialized;
        bool inside;

        public bool IsInside => inside;

        void OnEnable() {
            ResolvePlayer();
            Apply(true);
        }

        void LateUpdate() {
            if (!player || !player.gameObject.activeInHierarchy) ResolvePlayer();
            Apply(false);
        }

        void ResolvePlayer() {
            GameObject named = GameObject.Find("PLAYER");
            if (named) player = named.GetComponent<CoziPlayerV57>();
            if (player) return;
            player = FindFirstObjectByType<CoziPlayerV57>();
        }

        bool Contains(Vector3 p, float padding) {
            return p.x > minXZ.x - padding && p.x < maxXZ.x + padding &&
                   p.z > minXZ.y - padding && p.z < maxXZ.y + padding;
        }

        void Apply(bool force) {
            if (!player) return;
            bool next = Contains(player.transform.position, inside ? exitPadding : enterPadding);
            if (!force && initialized && next == inside) return;
            initialized = true;
            inside = next;
            if (exteriorShell) exteriorShell.SetActive(!inside);
            if (exteriorDetails) exteriorDetails.SetActive(!inside);
            if (interiorVisuals) interiorVisuals.SetActive(inside);
        }
    }
}
