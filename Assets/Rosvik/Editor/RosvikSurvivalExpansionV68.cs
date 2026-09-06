#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikSurvivalExpansionV68 {
        const int Version = 68;
        const string Key = "ROSVIK_SURVIVAL_EXPANSION_V68";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string Generated65 = "Assets/Rosvik/GeneratedV65";
        const string Generated64 = "Assets/Rosvik/GeneratedV64";
        const string Generated58 = "Assets/Rosvik/GeneratedV58";
        const string GroupName = "SURVIVAL GAMEPLAY V68";

        static RosvikSurvivalExpansionV68() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V68 BUILD REAL SURVIVAL LOOP")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V68 SURVIVAL EXPANSION FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject old = GameObject.Find(GroupName);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(GroupName);
            Transform loot = Group(root.transform, "LOOT + SEARCHABLES");
            Transform systems = Group(root.transform, "SURVIVAL SYSTEMS");

            Shader shader = PickShader();
            Material oak = LoadMat(Generated65 + "/oak.mat", Generated64 + "/dark_wood.mat", Generated58 + "/wood.mat");
            Material oakDark = LoadMat(Generated65 + "/oak_dark.mat", Generated64 + "/black.mat", Generated58 + "/metal.mat");
            Material ivory = LoadMat(Generated65 + "/ivory.mat", Generated64 + "/cream.mat", Generated58 + "/trim.mat");
            Material blue = LoadMat(Generated65 + "/school_blue.mat", Generated64 + "/soft_blue.mat", Generated58 + "/blue.mat");
            Material rust = LoadMat(Generated65 + "/rust.mat", Generated64 + "/burgundy.mat", Generated58 + "/red.mat");
            Material forest = LoadMat(Generated65 + "/forest.mat", Generated64 + "/forest.mat", Generated58 + "/wall_green.mat");
            Material metal = LoadMat(Generated65 + "/metal.mat", Generated64 + "/black.mat", Generated58 + "/metal.mat");
            Material glow = LoadMat(Generated65 + "/warm.mat", Generated64 + "/warm_glow.mat", Generated58 + "/warm_glow.mat");
            if (!oak) oak = MatTemp(shader, C("80502f"));
            if (!oakDark) oakDark = MatTemp(shader, C("3b2a20"));
            if (!ivory) ivory = MatTemp(shader, C("e9ddb9"));
            if (!blue) blue = MatTemp(shader, C("547982"));
            if (!rust) rust = MatTemp(shader, C("96533f"));
            if (!forest) forest = MatTemp(shader, C("2d493b"));
            if (!metal) metal = MatTemp(shader, C("263032"));
            if (!glow) glow = MatTemp(shader, C("ffad62"));

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER / CoziPlayerV57 not found in CozySchoolGame.");
            player.objective = "Hitta en ficklampa och en säkring. Sök skolan.";
            BuildFlashlight(player, systems);

            // A deliberate first survival sweep. Containers hug walls and unused corners; none are placed in circulation lanes.
            SupplyCrate(loot, "kvarglömt-skåpet", new Vector3(16.55f, 0, 2.95f), 180, oak, oakDark, ivory, blue,
                "Ficklampa", new[] { "Batterier", "Energibar" }, new[] { 2, 1 }, "Du har ljus. Hitta nu en säkring i vaktmästarrummet.");

            SupplyCrate(loot, "klassrum A materiallåda", new Vector3(-16.65f, 0, 6.05f), 90, oak, oakDark, ivory, blue,
                "Tejp", new[] { "Penna", "Förband" }, new[] { 1, 1 }, "");

            SupplyCrate(loot, "klassrum B elevlåda", new Vector3(-7.05f, 0, 6.05f), 90, oak, oakDark, ivory, rust,
                "Vattenflaska", new[] { "Energibar" }, new[] { 2 }, "");

            SupplyCrate(loot, "personalrummets skafferi", new Vector3(.85f, 0, 6.15f), 90, oak, oakDark, ivory, forest,
                "Konservburk", new[] { "Vattenflaska", "Tändare" }, new[] { 2, 1 }, "");

            SupplyCrate(loot, "bibliotekets hittegods", new Vector3(8.15f, 0, 6.05f), 90, oak, oakDark, ivory, blue,
                "Nyckelknippa", new[] { "Batterier" }, new[] { 1 }, "");

            SupplyCrate(loot, "vaktmästarens reservdelar", new Vector3(13.55f, 0, 6.05f), 90, oak, oakDark, ivory, rust,
                "Säkring", new[] { "Multiverktyg", "Batterier" }, new[] { 1, 2 }, "Du har säkringen. Gå till elskåpet i vaktmästarrummet.");

            SupplyCrate(loot, "sporthallens materiallåda", new Vector3(42.35f, 0, 13.75f), 180, oak, oakDark, ivory, forest,
                "Sporttejp", new[] { "Förband", "Vattenflaska" }, new[] { 2, 1 }, "");

            BuildPowerPanel(loot, new Vector3(17.68f, .62f, 8.6f), oakDark, ivory, rust, metal, glow);
            EnsureGameplayCollisions();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V68 SURVIVAL LOOP COMPLETE — real inventory, multi-item containers, flashlight/battery, objective chain, searchable school rooms and a collision pass are now active.");
        }

        static void BuildFlashlight(CoziPlayerV57 player, Transform p) {
            Transform old = player.transform.Find("V68 FLASHLIGHT");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject g = new GameObject("V68 FLASHLIGHT");
            g.transform.SetParent(player.transform, false);
            g.transform.localPosition = new Vector3(0, .72f, .18f);
            g.transform.localRotation = Quaternion.Euler(28f, 0, 0);
            Light l = g.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = C("ffd8a6");
            l.range = 14f;
            l.spotAngle = 62f;
            l.innerSpotAngle = 32f;
            l.intensity = 5.2f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = .35f;
            l.enabled = false;
            player.flashlight = l;
            EditorUtility.SetDirty(player);
        }

        static void SupplyCrate(Transform parent, string name, Vector3 pos, float yaw, Material wood, Material dark, Material trim, Material accent,
            string primary, string[] extras, int[] counts, string objectiveAfter) {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);

            GameObject body = LocalBox("crate body", root.transform, new Vector3(0, .33f, 0), new Vector3(1.22f, .58f, .76f), wood, true);
            LocalBox("crate front inset", root.transform, new Vector3(0, .36f, -.392f), new Vector3(.86f, .28f, .035f), dark, false);
            LocalBox("crate stripe", root.transform, new Vector3(0, .54f, -.414f), new Vector3(.90f, .07f, .025f), accent, false);
            LocalBox("crate foot L", root.transform, new Vector3(-.44f, .06f, 0), new Vector3(.12f, .12f, .64f), dark, true);
            LocalBox("crate foot R", root.transform, new Vector3(.44f, .06f, 0), new Vector3(.12f, .12f, .64f), dark, true);

            GameObject lidPivot = new GameObject("lid hinge");
            lidPivot.transform.SetParent(root.transform, false);
            lidPivot.transform.localPosition = new Vector3(0, .64f, .36f);
            GameObject lid = LocalBox("crate lid", lidPivot.transform, new Vector3(0, .04f, -.36f), new Vector3(1.28f, .10f, .80f), trim, true);
            LocalBox("lid accent", lidPivot.transform, new Vector3(0, .10f, -.36f), new Vector3(.88f, .035f, .48f), accent, false);

            GameObject contents = new GameObject("visible contents");
            contents.transform.SetParent(root.transform, false);
            LocalBox("contents A", contents.transform, new Vector3(-.28f, .66f, -.08f), new Vector3(.28f, .12f, .20f), accent, false);
            LocalBox("contents B", contents.transform, new Vector3(.10f, .66f, .06f), new Vector3(.22f, .12f, .26f), dark, false);
            LocalBox("contents C", contents.transform, new Vector3(.34f, .66f, -.11f), new Vector3(.18f, .12f, .18f), trim, false);

            CozyInteractableV57 x = root.AddComponent<CozyInteractableV57>();
            x.kind = CozyInteractableV57.Kind.Cabinet;
            x.displayName = name;
            x.itemName = primary;
            x.extraItems = extras ?? Array.Empty<string>();
            x.extraCounts = counts ?? Array.Empty<int>();
            x.radius = 2.0f;
            x.movingPart = lidPivot.transform;
            x.closedEuler = Vector3.zero;
            x.openEuler = new Vector3(-82f, 0, 0);
            x.revealOnOpen = contents.transform;
            x.highlightRenderer = body.GetComponent<Renderer>();
            x.animationTime = .28f;
            x.objectiveAfterUse = objectiveAfter;
        }

        static void BuildPowerPanel(Transform parent, Vector3 pos, Material dark, Material trim, Material rust, Material metal, Material glow) {
            GameObject root = new GameObject("ELSKÅP V68");
            root.transform.SetParent(parent, true);
            root.transform.position = pos;

            GameObject body = LocalBox("elskåp stomme", root.transform, Vector3.zero, new Vector3(.16f, 1.05f, 1.35f), dark, true);
            Transform hinge = Group(root.transform, "elskåp gångjärn");
            hinge.localPosition = new Vector3(-.10f, 0, -.66f);
            LocalBox("elskåp dörr", hinge, new Vector3(-.09f, 0, .66f), new Vector3(.08f, 1.02f, 1.32f), trim, true);
            LocalBox("varning", hinge, new Vector3(-.145f, .20f, .66f), new Vector3(.025f, .20f, .34f), rust, false);
            LocalBox("handtag", hinge, new Vector3(-.16f, 0, 1.17f), new Vector3(.06f, .25f, .06f), metal, false);

            GameObject inside = new GameObject("säkringsinnehåll");
            inside.transform.SetParent(root.transform, false);
            for (int i = 0; i < 5; i++)
                LocalBox("säkring", inside.transform, new Vector3(-.12f, .28f - i * .14f, -.25f + (i%2)*.45f), new Vector3(.10f, .09f, .22f), i == 2 ? rust : trim, false);

            CozyInteractableV57 x = root.AddComponent<CozyInteractableV57>();
            x.kind = CozyInteractableV57.Kind.Cabinet;
            x.displayName = "elskåpet";
            x.requiredItem = "Säkring";
            x.radius = 2.1f;
            x.movingPart = hinge;
            x.closedEuler = Vector3.zero;
            x.openEuler = new Vector3(0, -105f, 0);
            x.revealOnOpen = inside.transform;
            x.highlightRenderer = body.GetComponent<Renderer>();
            x.animationTime = .30f;
            x.objectiveAfterUse = "Säkringen sitter i. Sök sporthallen och samla det du behöver innan du lämnar skolan.";
        }

        static void EnsureGameplayCollisions() {
            string[] solid = { "desk", "table", "shelf", "cabinet", "cubby", "couch", "sofa", "armchair", "workbench", "bench", "crate body" };
            string[] ignore = { "rug", "runner", "book", "plant", "cactus", "lamp", "poster", "picture", "window", "notice", "light", "floor", "paper", "contents" };
            int added = 0;
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!t || !t.gameObject.scene.IsValid()) continue;
                string n = t.name.ToLowerInvariant();
                bool wants = false;
                foreach (string s in solid) if (n.Contains(s)) { wants = true; break; }
                if (!wants) continue;
                bool skip = false;
                foreach (string s in ignore) if (n.Contains(s)) { skip = true; break; }
                if (skip) continue;
                if (t.GetComponent<Collider>() || t.GetComponentInChildren<Collider>()) continue;
                Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                BoxCollider c = t.gameObject.AddComponent<BoxCollider>();
                c.center = t.InverseTransformPoint(b.center);
                Vector3 lossy = t.lossyScale;
                c.size = new Vector3(
                    Mathf.Abs(lossy.x) > .0001f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x,
                    Mathf.Abs(lossy.y) > .0001f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y,
                    Mathf.Abs(lossy.z) > .0001f ? b.size.z / Mathf.Abs(lossy.z) : b.size.z);
                added++;
            }
            Debug.Log("V68 collision pass added " + added + " missing furniture colliders.");
        }

        static Shader PickShader() {
            Shader s = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (!s || !s.isSupported) s = Shader.Find("Standard");
            return s;
        }
        static Material LoadMat(params string[] paths) { foreach (string p in paths) { Material m = AssetDatabase.LoadAssetAtPath<Material>(p); if (m) return m; } return null; }
        static Material MatTemp(Shader shader, Color c) { Material m = new Material(shader); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c); return m; }
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        static Transform Group(Transform p, string name) { GameObject g = new GameObject(name); g.transform.SetParent(p, false); return g.transform; }
        static GameObject LocalBox(string name, Transform p, Vector3 pos, Vector3 size, Material mat, bool collider) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) { Collider c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c); } return g;
        }
    }
}
#endif
