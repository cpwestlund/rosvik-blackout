using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(12850)]
    public sealed class HouseEnvelopeV84 : MonoBehaviour {
        public GameObject roofVisual;
        public GameObject cameraFacingExterior;
        public GameObject interiorDetails;
        public Vector2 minXZ;
        public Vector2 maxXZ;
        public float enterPadding = .85f;
        public float exitPadding = 1.35f;

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
            CoziPlayerV57[] all = FindObjectsByType<CoziPlayerV57>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++) {
                if (!all[i] || !all[i].gameObject.activeInHierarchy) continue;
                player = all[i];
                if (all[i].name.Equals("PLAYER", StringComparison.OrdinalIgnoreCase)) break;
            }
        }

        bool Contains(Vector3 p, float padding) {
            return p.x > minXZ.x - padding && p.x < maxXZ.x + padding &&
                   p.z > minXZ.y - padding && p.z < maxXZ.y + padding;
        }

        void Apply(bool force) {
            if (!player) return;
            float pad = inside ? exitPadding : enterPadding;
            bool nowInside = Contains(player.transform.position, pad);
            if (!force && initialized && nowInside == inside) return;
            initialized = true;
            inside = nowInside;
            if (roofVisual) roofVisual.SetActive(!inside);
            if (cameraFacingExterior) cameraFacingExterior.SetActive(!inside);
            if (interiorDetails) interiorDetails.SetActive(inside);
        }
    }

    [DefaultExecutionOrder(-9250)]
    public sealed class ItemDescriptionPolishV84 : MonoBehaviour {
        static bool applied;

        void Awake() {
            if (applied) return;
            applied = true;
            FieldInfo f = typeof(SurvivalSystemsV69).GetField("defs", BindingFlags.Static | BindingFlags.NonPublic);
            var defs = f != null ? f.GetValue(null) as Dictionary<string, SurvivalSystemsV69.ItemDef> : null;
            if (defs == null) return;
            Set(defs, "Handskar", "Slitna arbetshandskar med gummerade handflator. Värmen är modest men de skyddar händerna när du arbetar ute.");
            Set(defs, "Tejp", "En halv rulle kraftig tejp. Limmet blir stelt i kylan men tejpen kan rädda både utrustning och provisoriska lagningar.");
            Set(defs, "Multiverktyg", "Tång, kniv och mejslar i ett kompakt verktyg. Inte bäst på något, men användbart till nästan allt.");
            Set(defs, "Vattenflaska", "Förseglad vattenflaska. Kylan har börjat lägga is i flaskhalsen.");
            Set(defs, "Kex", "Torra, hållbara kex. Lätta att bära men de gör dig törstigare.");
            Set(defs, "Batterier", "Alkaliska reservbatterier. Laddningen är osäker tills du faktiskt provar dem.");
            Set(defs, "Förband", "Förbandsmaterial med kompresser och tejp. Avsett för att stoppa blödning och skydda ett sår.");
            Set(defs, "Tändare", "En enkel tändare. I strömavbrottet är en liten låga betydligt viktigare än den ser ut.");
            Set(defs, "Ulltröja", "Tjock ulltröja. Den behåller en stor del av sin isolering även när den blivit fuktig.");
            Set(defs, "Kängor", "Stadiga vinterkängor med grov sula. Skyddar bättre mot väta, kyla och skräp på marken.");
        }

        static void Set(Dictionary<string, SurvivalSystemsV69.ItemDef> defs, string item, string description) {
            if (defs.TryGetValue(item, out var d) && d != null) d.description = description;
        }
    }

    [DefaultExecutionOrder(12900)]
    public sealed class WinterPresentationV84 : MonoBehaviour {
        public bool useFog = true;
        public float fogDensity = .0028f;
        SurvivalSystemsV69 survival;

        void Awake() {
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            RenderSettings.fog = useFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }

        void LateUpdate() {
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            if (!survival) return;
            Color target;
            switch (survival.weather) {
                case SurvivalSystemsV69.WeatherKind.Clear: target = new Color(.39f,.44f,.46f); break;
                case SurvivalSystemsV69.WeatherKind.Rain: target = new Color(.28f,.35f,.38f); break;
                case SurvivalSystemsV69.WeatherKind.Snow: target = new Color(.31f,.39f,.44f); break;
                case SurvivalSystemsV69.WeatherKind.ColdSnap: target = new Color(.22f,.30f,.37f); break;
                default: target = new Color(.32f,.38f,.40f); break;
            }
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, target, Time.deltaTime * .35f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, target * .78f, Time.deltaTime * .25f);
            if (useFog) {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogDensity, Time.deltaTime * .25f);
            }
        }
    }
}
