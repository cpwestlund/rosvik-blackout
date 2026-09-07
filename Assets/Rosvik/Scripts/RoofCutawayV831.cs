using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(12500)]
    public sealed class RoofCutawayV831 : MonoBehaviour {
        public GameObject roofVisual;
        public Vector2 minXZ;
        public Vector2 maxXZ;
        public float inset = -0.08f;

        CoziPlayerV57 player;
        bool initialized;
        bool lastInside;

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

            CoziPlayerV57[] all = FindObjectsByType<CoziPlayerV57>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++) {
                if (!all[i] || !all[i].gameObject.activeInHierarchy) continue;
                player = all[i];
                if (all[i].name.Equals("PLAYER", System.StringComparison.OrdinalIgnoreCase)) break;
            }
        }

        bool PlayerInside() {
            if (!player) return false;
            Vector3 p = player.transform.position;
            float minX = minXZ.x - inset;
            float maxX = maxXZ.x + inset;
            float minZ = minXZ.y - inset;
            float maxZ = maxXZ.y + inset;
            return p.x > minX && p.x < maxX && p.z > minZ && p.z < maxZ;
        }

        void Apply(bool force) {
            if (!roofVisual) return;
            bool inside = PlayerInside();
            if (!force && initialized && inside == lastInside) return;
            initialized = true;
            lastInside = inside;
            roofVisual.SetActive(!inside);
        }
    }
}
