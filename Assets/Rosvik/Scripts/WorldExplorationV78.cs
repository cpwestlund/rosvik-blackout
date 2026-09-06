using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9300)]
    public sealed class WorldExplorationV78 : MonoBehaviour {
        public Vector2 garageMinXZ = new Vector2(51.7f, 4.55f);
        public Vector2 garageMaxXZ = new Vector2(65.2f, 15.25f);

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        bool garageDiscovered;
        bool lastGarage;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
        }

        void LateUpdate() {
            if (!player || !survival) return;
            bool inGarage = InsideGarage(player.transform.position);

            if (inGarage) {
                // The garage is a real shelter, not just scenery.
                survival.wetness = Mathf.MoveTowards(survival.wetness, 0f, 1.15f * Time.deltaTime);
                survival.warmth = Mathf.MoveTowards(survival.warmth, 69f, .12f * Time.deltaTime);
                foreach (string n in new[]{"V69 RAIN","V69 SNOW","V70 RAIN","V70 SNOW","V72 RAIN","V72 SNOW"}) {
                    Transform t = player.transform.Find(n);
                    if (t && t.gameObject.activeSelf) t.gameObject.SetActive(false);
                }
            }

            if (inGarage && !garageDiscovered) {
                garageDiscovered = true;
                player.ShowToast("GARAGET — verktyg, bränsle och reservdelar", 2.8f);
                player.SetObjective("Sök igenom garaget. Verktyg och bränsle finns oftare i verkstäder än i bostäder.");
            }

            if (lastGarage && !inGarage && garageDiscovered) {
                player.SetObjective("Fortsätt utforska området. Håll koll på väder, skador, utrustning och vad ryggsäcken väger.");
            }
            lastGarage = inGarage;
        }

        bool InsideGarage(Vector3 p) {
            return p.x > garageMinXZ.x && p.x < garageMaxXZ.x && p.z > garageMinXZ.y && p.z < garageMaxXZ.y;
        }
    }
}
