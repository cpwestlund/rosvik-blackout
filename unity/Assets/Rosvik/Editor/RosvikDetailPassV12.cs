#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikDetailPassV12 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string VersionKey = "ROSVIK_DETAIL_PASS_VERSION";
        const string GeneratedFolder = "Assets/Rosvik/Generated/V12";
        const int Version = 12;

        static RosvikDetailPassV12() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Detail Pass V12")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(VersionKey, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                RosvikSiteCalibrationV11.Apply();
                if (!File.Exists(ScenePath)) return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) {
                    Debug.LogWarning("ROSVIK V12: V11 root not found yet; retry after scripts finish compiling.");
                    return;
                }

                EnsureGeneratedFolder();

                Texture2D woodTex = ProceduralTexture("TimberPanels", TextureKind.Wood);
                Texture2D brickTex = ProceduralTexture("SeventiesMasonry", TextureKind.Brick);
                Texture2D plasterTex = ProceduralTexture("StonePlaster", TextureKind.Plaster);
                Texture2D roofTex = ProceduralTexture("DarkMetalRoof", TextureKind.Metal);
                Texture2D snowTex = ProceduralTexture("SnowSurface", TextureKind.Snow);
                Texture2D packedTex = ProceduralTexture("PackedSnow", TextureKind.PackedSnow);

                Material timberMat = MaterialAsset("TimberYellow", new Color(.70f,.57f,.30f), woodTex, .10f, new Vector2(4.2f,1.7f));
                Material timberShade = MaterialAsset("TimberGroove", new Color(.42f,.31f,.14f), woodTex, .07f, new Vector2(4.2f,1.7f));
                Material oldWhite = MaterialAsset("OldWhitePaint", new Color(.83f,.82f,.75f), plasterTex, .18f, new Vector2(2f,2f));
                Material mainMat = MaterialAsset("SeventiesFacade", new Color(.48f,.44f,.35f), brickTex, .08f, new Vector2(5.4f,2.5f));
                Material mainBase = MaterialAsset("SeventiesDarkBase", new Color(.27f,.25f,.22f), brickTex, .05f, new Vector2(5.4f,2.1f));
                Material stoneMat = MaterialAsset("StoneSchoolPlaster", new Color(.56f,.56f,.52f), plasterTex, .06f, new Vector2(3.5f,2.5f));
                Material stoneBase = MaterialAsset("StoneSchoolBase", new Color(.33f,.34f,.33f), brickTex, .05f, new Vector2(4f,2f));
                Material metalRoof = MaterialAsset("AgedMetalRoof", new Color(.105f,.115f,.12f), roofTex, .30f, new Vector2(5f,3f));
                Material snowMat = MaterialAsset("DetailedSnow", new Color(.91f,.94f,.96f), snowTex, .18f, new Vector2(10f,10f));
                Material packedSnow = MaterialAsset("DetailedPackedSnow", new Color(.73f,.78f,.80f), packedTex, .09f, new Vector2(9f,6f));
                Material dirtySnow = MaterialAsset("PloughEdgeSnow", new Color(.68f,.70f,.69f), packedTex, .05f, new Vector2(7f,5f));
                Material darkMetal = MaterialAsset("DarkMetal", new Color(.11f,.13f,.14f), roofTex, .24f, new Vector2(2f,2f));
                Material woodDark = MaterialAsset("BenchWood", new Color(.28f,.19f,.12f), woodTex, .13f, new Vector2(3f,1f));
                Material spruceMat = MaterialAsset("SpruceNeedles", new Color(.085f,.16f,.13f), plasterTex, .06f, new Vector2(4f,4f));
                Material barkMat = MaterialAsset("Bark", new Color(.18f,.13f,.095f), woodTex, .04f, new Vector2(2f,3f));
                Material birchMat = MaterialAsset("BirchBark", new Color(.77f,.76f,.69f), plasterTex, .08f, new Vector2(1f,4f));
                Material glass = MaterialAsset("ColdWindowGlassV12", new Color(.14f,.24f,.28f), plasterTex, .52f, new Vector2(1f,1f));
                Material warm = MaterialAsset("WarmEntranceLight", new Color(1f,.55f,.22f), plasterTex, .42f, new Vector2(1f,1f));

                ApplyGroundMaterials(root, snowMat, packedSnow, dirtySnow);

                Transform main = Child(root, "Huvudbyggnaden - 1970-tal");
                Transform timber = Child(root, "Träskolan - ca 1900");
                Transform stone = Child(root, "Stenskolan - 1940/50-tal");
                Transform module = Child(root, "Tillfällig skolmodul");
                Transform arena = Child(root, "Norrbotten Stål Arena - background landmark");

                if (main) DetailMainSchool(main, mainMat, mainBase, metalRoof, snowMat, darkMetal, glass, warm);
                if (timber) DetailTimberSchool(timber, timberMat, timberShade, oldWhite, metalRoof, snowMat, darkMetal, glass, warm);
                if (stone) DetailStoneSchool(stone, stoneMat, stoneBase, metalRoof, snowMat, darkMetal, glass, warm);
                if (module) DetailModule(module, mainMat, darkMetal, glass);
                if (arena) DetailArena(arena, mainBase, metalRoof, snowMat, darkMetal);

                ReplacePrototypeTrees(root, spruceMat, barkMat, snowMat, birchMat, darkMetal);
                DetailSchoolyard(root, packedSnow, dirtySnow, darkMetal, woodDark, snowMat, warm);
                TuneLighting(root);

                root.name = "ROSVIK_DETAIL_PASS_V12";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(VersionKey, Version);
                Debug.Log("ROSVIK V12 APPLIED: textured facades, roof snow, drainage, winter trees, yard props and first hero-detail pass.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V12 DETAIL PASS FAILED: " + ex);
            }
        }

        enum TextureKind { Wood, Brick, Plaster, Metal, Snow, PackedSnow }

        static Transform FindRoot() {
            string[] names = { "ROSVIK_DETAIL_PASS_V12", "ROSVIK_SITE_CALIBRATION_V11", "ROSVIK_REAL_CAMPUS_V10" };
            foreach (string n in names) {
                GameObject go = GameObject.Find(n);
                if (go) return go.transform;
            }
            return null;
        }

        static Transform Child(Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static void DestroyByPrefix(Transform root, string prefix) {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--) {
                Transform t = all[i];
                if (t == root) continue;
                if (t.name.StartsWith(prefix, StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        static Renderer RendererOf(Transform root, string name) {
            Transform t = Child(root, name);
            return t ? t.GetComponent<Renderer>() : null;
        }

        static void SetMaterial(Transform root, string objectName, Material mat) {
            Renderer r = RendererOf(root, objectName);
            if (r) r.sharedMaterial = mat;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, bool collider = false) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = rot;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void EnsureGeneratedFolder() {
            if (!AssetDatabase.IsValidFolder("Assets/Rosvik/Generated")) {
                if (!AssetDatabase.IsValidFolder("Assets/Rosvik")) AssetDatabase.CreateFolder("Assets", "Rosvik");
                AssetDatabase.CreateFolder("Assets/Rosvik", "Generated");
            }
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder("Assets/Rosvik/Generated", "V12");
        }

        static Texture2D ProceduralTexture(string name, TextureKind kind) {
            string path = GeneratedFolder + "/" + name + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float n = HashNoise(x, y);
                    float v = .78f;
                    switch (kind) {
                        case TextureKind.Wood: {
                            int p = x % 16;
                            float seam = p <= 1 ? -.28f : (p == 2 ? -.10f : 0f);
                            float grain = Mathf.Sin(y * .35f + Mathf.Sin(x * .17f) * 2f) * .035f;
                            v = .82f + seam + grain + n * .07f;
                            break;
                        }
                        case TextureKind.Brick: {
                            int row = y / 16;
                            int ox = (row % 2) * 15;
                            int bx = (x + ox) % 30;
                            int by = y % 16;
                            bool mortar = bx < 2 || by < 2;
                            v = mortar ? .48f : .79f + n * .10f;
                            break;
                        }
                        case TextureKind.Plaster:
                            v = .80f + n * .12f + Mathf.Sin((x + y) * .16f) * .018f;
                            break;
                        case TextureKind.Metal: {
                            int seam = x % 12;
                            float ridge = seam < 2 ? .14f : (seam < 4 ? .04f : 0f);
                            v = .72f + ridge + n * .035f;
                            break;
                        }
                        case TextureKind.Snow:
                            v = .91f + n * .045f + Mathf.Sin(x * .15f + y * .11f) * .012f;
                            break;
                        case TextureKind.PackedSnow: {
                            float tracks = ((x > 35 && x < 43) || (x > 83 && x < 91)) ? -.08f : 0f;
                            v = .82f + tracks + n * .07f;
                            break;
                        }
                    }
                    v = Mathf.Clamp01(v);
                    tex.SetPixel(x, y, new Color(v, v, v));
                }
            }
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static float HashNoise(int x, int y) {
            unchecked {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return ((h & 1023) / 1023f) - .5f;
            }
        }

        static Material MaterialAsset(string name, Color tint, Texture2D tex, float smooth, Vector2 tiling) {
            string path = GeneratedFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) {
                Shader shader = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_BaseMap")) {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
            } else {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
            }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", name.Contains("Metal") || name.Contains("Roof") ? .18f : 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ApplyGroundMaterials(Transform root, Material snow, Material packed, Material dirty) {
            SetMaterial(root, "Snow terrain", snow);
            SetMaterial(root, "Main cleared yard", packed);
            SetMaterial(root, "Large central schoolyard", packed);
            SetMaterial(root, "West play yard", packed);
            SetMaterial(root, "Broad uphill transition", packed);
            SetMaterial(root, "Upper Träskolan terrace", packed);
            SetMaterial(root, "Stenskolan terrace", packed);
            SetMaterial(root, "Path uphill to Träskolan", dirty);
            SetMaterial(root, "Gentle path to Stenskolan", dirty);

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) {
                if (t.name.StartsWith("Ploughed snow bank", StringComparison.Ordinal)) {
                    Renderer r = t.GetComponent<Renderer>();
                    if (r) r.sharedMaterial = dirty;
                }
            }
        }

        static void DetailMainSchool(Transform b, Material facade, Material baseMat, Material roof, Material snow, Material metal, Material glass, Material warm) {
            SetMaterial(b, "Main mass", facade);
            SetMaterial(b, "Dark foundation", baseMat);
            SetMaterial(b, "Roof", roof);
            ReplaceWindowGlass(b, glass);
            DestroyByPrefix(b, "V12 main");

            for (float x = -16.5f; x <= 16.5f; x += 2.75f)
                Cube("V12 main facade joint", b, new Vector3(x, 1.85f, 5.48f), new Vector3(.045f, 2.55f, .045f), baseMat, false);
            Cube("V12 main fascia", b, new Vector3(0f, 3.18f, 5.56f), new Vector3(36f, .18f, .16f), metal, false);
            GutterRun("V12 main gutter", b, 36f, 3.30f, 5.82f, metal);
            Downpipe("V12 main downpipe L", b, new Vector3(-17.4f,1.55f,5.73f), 3.05f, metal);
            Downpipe("V12 main downpipe R", b, new Vector3(17.4f,1.55f,5.73f), 3.05f, metal);

            AddRoofSnow(b, 35f, 10.8f, 3.25f, 1.15f, .45f, snow, "V12 main roof snow");
            Cube("V12 main roof vent A", b, new Vector3(-7f, 4.55f, -.8f), new Vector3(1.35f,.55f,1.10f), metal, false);
            Cube("V12 main roof vent B", b, new Vector3(5.8f, 4.54f, 1.2f), new Vector3(1.05f,.48f,.95f), metal, false);
            Cylinder("V12 main exhaust", b, new Vector3(11.2f,4.62f,-1.6f), new Vector3(.18f,.45f,.18f), Quaternion.identity, metal, false);

            EntranceLamp(b, new Vector3(-10.8f,2.72f,5.76f), warm, metal);
            EntranceLamp(b, new Vector3(-1.2f,2.70f,5.76f), warm, metal);
        }

        static void DetailTimberSchool(Transform b, Material timber, Material groove, Material white, Material roof, Material snow, Material metal, Material glass, Material warm) {
            SetMaterial(b, "Two storey timber body", timber);
            SetMaterial(b, "Roof", roof);
            SetMaterial(b, "Stone plinth", MaterialAsset("TimberSchoolPlinth", new Color(.38f,.39f,.37f), ProceduralTexture("StonePlaster", TextureKind.Plaster), .06f, new Vector2(3f,2f)));
            ReplaceWindowGlass(b, glass);
            DestroyByPrefix(b, "V11 vertical timber batten");
            DestroyByPrefix(b, "V12 timber");

            for (float x = -6.8f; x <= 6.8f; x += .72f) {
                Cube("V12 timber board groove front", b, new Vector3(x,3.10f,4.325f), new Vector3(.025f,5.45f,.035f), groove, false);
                Cube("V12 timber board groove back", b, new Vector3(x,3.10f,-4.325f), new Vector3(.025f,5.45f,.035f), groove, false);
            }
            for (float z = -3.8f; z <= 3.8f; z += .72f) {
                Cube("V12 timber board groove left", b, new Vector3(-7.275f,3.10f,z), new Vector3(.035f,5.45f,.025f), groove, false);
                Cube("V12 timber board groove right", b, new Vector3(7.275f,3.10f,z), new Vector3(.035f,5.45f,.025f), groove, false);
            }

            Cube("V12 timber white belt front", b, new Vector3(0f,3.04f,4.38f), new Vector3(14.7f,.16f,.11f), white, false);
            Cube("V12 timber white belt back", b, new Vector3(0f,3.04f,-4.38f), new Vector3(14.7f,.16f,.11f), white, false);
            Cube("V12 timber corner FL", b, new Vector3(-7.30f,3.1f,4.38f), new Vector3(.20f,5.7f,.12f), white, false);
            Cube("V12 timber corner FR", b, new Vector3(7.30f,3.1f,4.38f), new Vector3(.20f,5.7f,.12f), white, false);
            Cube("V12 timber corner BL", b, new Vector3(-7.30f,3.1f,-4.38f), new Vector3(.20f,5.7f,.12f), white, false);
            Cube("V12 timber corner BR", b, new Vector3(7.30f,3.1f,-4.38f), new Vector3(.20f,5.7f,.12f), white, false);

            GutterRun("V12 timber gutter front", b, 15.5f, 6.02f, 4.85f, metal);
            GutterRun("V12 timber gutter back", b, 15.5f, 6.02f, -4.85f, metal);
            Downpipe("V12 timber downpipe FL", b, new Vector3(-7.35f,2.75f,4.68f), 5.4f, metal);
            Downpipe("V12 timber downpipe FR", b, new Vector3(7.35f,2.75f,4.68f), 5.4f, metal);

            AddRoofSnow(b, 14.5f, 8.6f, 6.0f, 3.15f, .55f, snow, "V12 timber roof snow");
            Cube("V12 timber chimney A", b, new Vector3(-3.4f,8.25f,-.65f), new Vector3(.65f,1.05f,.65f), groove, false);
            Cube("V12 timber chimney cap A", b, new Vector3(-3.4f,8.82f,-.65f), new Vector3(.78f,.12f,.78f), metal, false);
            Cube("V12 timber chimney B", b, new Vector3(3.1f,8.15f,.55f), new Vector3(.62f,.95f,.62f), groove, false);
            Cube("V12 timber chimney cap B", b, new Vector3(3.1f,8.67f,.55f), new Vector3(.75f,.12f,.75f), metal, false);

            Cube("V12 timber porch step lower", b, new Vector3(0f,.12f,5.57f), new Vector3(3.4f,.20f,1.25f), white, true);
            Cube("V12 timber porch step upper", b, new Vector3(0f,.25f,5.02f), new Vector3(3.0f,.20f,.72f), white, true);
            Railing("V12 timber rail L", b, new Vector3(-1.47f,.92f,5.42f), 1.55f, metal);
            Railing("V12 timber rail R", b, new Vector3(1.47f,.92f,5.42f), 1.55f, metal);
            EntranceLamp(b, new Vector3(0f,2.92f,4.54f), warm, metal);
        }

        static void DetailStoneSchool(Transform b, Material facade, Material baseMat, Material roof, Material snow, Material metal, Material glass, Material warm) {
            SetMaterial(b, "Masonry body", facade);
            SetMaterial(b, "Foundation", baseMat);
            SetMaterial(b, "Roof", roof);
            ReplaceWindowGlass(b, glass);
            DestroyByPrefix(b, "V12 stone");

            Cube("V12 stone plinth front", b, new Vector3(0f,.58f,4.16f), new Vector3(17.7f,1.0f,.12f), baseMat, false);
            Cube("V12 stone sill band", b, new Vector3(0f,1.12f,4.23f), new Vector3(17.7f,.10f,.08f), metal, false);
            Cube("V12 stone eave band", b, new Vector3(0f,4.02f,4.25f), new Vector3(17.8f,.15f,.10f), metal, false);
            GutterRun("V12 stone gutter", b, 18.4f, 4.22f, 4.70f, metal);
            Downpipe("V12 stone downpipe L", b, new Vector3(-8.55f,2.05f,4.52f), 4.0f, metal);
            Downpipe("V12 stone downpipe R", b, new Vector3(8.55f,2.05f,4.52f), 4.0f, metal);
            AddRoofSnow(b, 17.5f, 8.2f, 4.2f, 2.35f, .48f, snow, "V12 stone roof snow");
            EntranceLamp(b, new Vector3(-6.2f,2.70f,4.45f), warm, metal);
        }

        static void DetailModule(Transform b, Material facade, Material metal, Material glass) {
            SetMaterial(b, "Module body", facade);
            SetMaterial(b, "Module roof", metal);
            ReplaceWindowGlass(b, glass);
            DestroyByPrefix(b, "V12 module");
            for (float x=-5.8f; x<=5.8f; x+=2.9f)
                Cube("V12 module seam", b, new Vector3(x,1.55f,2.63f), new Vector3(.04f,2.65f,.04f), metal, false);
        }

        static void DetailArena(Transform b, Material facade, Material roof, Material snow, Material metal) {
            SetMaterial(b, "Arena hall", facade);
            SetMaterial(b, "Arena roof", roof);
            DestroyByPrefix(b, "V12 arena");
            Cube("V12 arena snow cap", b, new Vector3(0f,7.73f,0f), new Vector3(28.2f,.08f,16.2f), snow, false);
            for (float x=-11f; x<=11f; x+=5.5f)
                Cube("V12 arena facade rib", b, new Vector3(x,3.8f,7.54f), new Vector3(.10f,6.6f,.08f), metal, false);
        }

        static void ReplaceWindowGlass(Transform b, Material glass) {
            foreach (Transform t in b.GetComponentsInChildren<Transform>(true)) {
                if (t.name == "Glass" || t.name == "Door glass upper") {
                    Renderer r = t.GetComponent<Renderer>();
                    if (r) r.sharedMaterial = glass;
                }
            }
        }

        static void AddRoofSnow(Transform b, float width, float depth, float wallH, float rise, float overhang, Material snow, string prefix) {
            float halfRun = depth * .5f + overhang;
            float angle = Mathf.Atan2(rise, halfRun) * Mathf.Rad2Deg;
            float slope = Mathf.Sqrt(halfRun * halfRun + rise * rise);
            float cy = wallH + rise * .5f + .11f;
            float cz = halfRun * .5f;
            GameObject back = Cube(prefix + " back", b, new Vector3(0f,cy,-cz), new Vector3(width+overhang*2f,.07f,slope), snow, false);
            back.transform.localRotation = Quaternion.Euler(-angle,0f,0f);
            GameObject front = Cube(prefix + " front", b, new Vector3(0f,cy,cz), new Vector3(width+overhang*2f,.07f,slope), snow, false);
            front.transform.localRotation = Quaternion.Euler(angle,0f,0f);
        }

        static void GutterRun(string name, Transform parent, float length, float y, float z, Material metal) {
            Cylinder(name, parent, new Vector3(0f,y,z), new Vector3(.07f,length*.5f,.07f), Quaternion.Euler(0f,0f,90f), metal, false);
        }

        static void Downpipe(string name, Transform parent, Vector3 pos, float height, Material metal) {
            Cylinder(name, parent, pos, new Vector3(.065f,height*.5f,.065f), Quaternion.identity, metal, false);
        }

        static void Railing(string prefix, Transform parent, Vector3 center, float run, Material metal) {
            Cylinder(prefix + " post A", parent, center + new Vector3(0f,0f,-run*.5f), new Vector3(.035f,.58f,.035f), Quaternion.identity, metal, false);
            Cylinder(prefix + " post B", parent, center + new Vector3(0f,0f,run*.5f), new Vector3(.035f,.58f,.035f), Quaternion.identity, metal, false);
            Cylinder(prefix + " top", parent, center + new Vector3(0f,.55f,0f), new Vector3(.035f,run*.5f,.035f), Quaternion.Euler(90f,0f,0f), metal, false);
        }

        static void EntranceLamp(Transform parent, Vector3 pos, Material glow, Material metal) {
            Cube("V12 entrance lamp housing", parent, pos, new Vector3(.32f,.20f,.18f), metal, false);
            Cube("V12 entrance lamp glow", parent, pos + new Vector3(0f,-.03f,.105f), new Vector3(.22f,.10f,.035f), glow, false);
        }

        static void ReplacePrototypeTrees(Transform root, Material needles, Material bark, Material snow, Material birch, Material branch) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) {
                if (t.parent == root && t.name == "Spruce") t.gameObject.SetActive(false);
            }
            DestroyByPrefix(root, "V12 spruce");
            DestroyByPrefix(root, "V12 birch");

            Mesh cone = ConeMeshAsset();
            Vector3[] sprucePositions = {
                new Vector3(-48f,0f,-40f), new Vector3(-42f,0f,-46f), new Vector3(-34f,.72f,-50f),
                new Vector3(-20f,.3f,-53f), new Vector3(6f,.15f,-51f), new Vector3(39f,.25f,-47f),
                new Vector3(52f,0f,-22f), new Vector3(-54f,0f,3f), new Vector3(-45f,0f,29f),
                new Vector3(46f,0f,31f), new Vector3(57f,0f,11f)
            };
            for (int i=0;i<sprucePositions.Length;i++)
                Spruce(root, sprucePositions[i], 1.0f + (i%4)*.12f, cone, needles, bark, snow, "V12 spruce " + i);

            Vector3[] birches = {
                new Vector3(-22f,.20f,-18f), new Vector3(-13f,.12f,-20f), new Vector3(12f,.10f,-18f),
                new Vector3(22f,.14f,-15f), new Vector3(-36f,.72f,-32f), new Vector3(35f,.30f,-31f)
            };
            for (int i=0;i<birches.Length;i++)
                Birch(root, birches[i], 1f + (i%3)*.10f, birch, branch, "V12 birch " + i);
        }

        static Mesh ConeMeshAsset() {
            string path = GeneratedFolder + "/SpruceCone.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;

            const int seg = 14;
            Vector3[] v = new Vector3[seg + 2];
            int[] tri = new int[seg * 6];
            v[0] = new Vector3(0f,1f,0f);
            v[1] = Vector3.zero;
            for (int i=0;i<seg;i++) {
                float a = i * Mathf.PI * 2f / seg;
                v[i+2] = new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            }
            int ti=0;
            for (int i=0;i<seg;i++) {
                int a=2+i, b=2+((i+1)%seg);
                tri[ti++]=0; tri[ti++]=b; tri[ti++]=a;
                tri[ti++]=1; tri[ti++]=a; tri[ti++]=b;
            }
            Mesh mesh = new Mesh { name = "SpruceCone" };
            mesh.vertices=v; mesh.triangles=tri; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }

        static void Spruce(Transform parent, Vector3 pos, float scale, Mesh cone, Material needles, Material bark, Material snow, string name) {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent,false); root.localPosition=pos;
            Cylinder("Trunk", root, new Vector3(0f,2.0f*scale,0f), new Vector3(.18f,2.0f*scale,.18f), Quaternion.identity, bark, false);
            for (int i=0;i<4;i++) {
                float y=(1.2f+i*.92f)*scale;
                float radius=(2.0f-i*.32f)*scale;
                float height=2.6f*scale;
                GameObject layer = new GameObject("Branch layer");
                layer.transform.SetParent(root,false);
                layer.transform.localPosition=new Vector3(0f,y,0f);
                layer.transform.localScale=new Vector3(radius,height,radius);
                layer.AddComponent<MeshFilter>().sharedMesh=cone;
                layer.AddComponent<MeshRenderer>().sharedMaterial=needles;
                if (i<2) {
                    GameObject cap = new GameObject("Snow on branches");
                    cap.transform.SetParent(root,false);
                    cap.transform.localPosition=new Vector3(0f,y+.10f*scale,0f);
                    cap.transform.localScale=new Vector3(radius*.96f,.18f*scale,radius*.96f);
                    cap.AddComponent<MeshFilter>().sharedMesh=cone;
                    cap.AddComponent<MeshRenderer>().sharedMaterial=snow;
                }
            }
        }

        static void Birch(Transform parent, Vector3 pos, float scale, Material bark, Material branch, string name) {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent,false); root.localPosition=pos;
            Cylinder("Birch trunk", root, new Vector3(0f,2.6f*scale,0f), new Vector3(.13f,2.6f*scale,.13f), Quaternion.identity, bark, false);
            AddBranch(root,new Vector3(0f,3.7f*scale,0f),new Vector3(.06f,1.0f*scale,.06f),Quaternion.Euler(0f,0f,-38f),branch);
            AddBranch(root,new Vector3(0f,4.2f*scale,0f),new Vector3(.055f,.85f*scale,.055f),Quaternion.Euler(22f,0f,42f),branch);
            AddBranch(root,new Vector3(0f,4.7f*scale,0f),new Vector3(.045f,.72f*scale,.045f),Quaternion.Euler(-25f,15f,-47f),branch);
            AddBranch(root,new Vector3(0f,5.0f*scale,0f),new Vector3(.04f,.55f*scale,.04f),Quaternion.Euler(35f,-20f,25f),branch);
        }

        static void AddBranch(Transform root, Vector3 pos, Vector3 scale, Quaternion rot, Material mat) {
            Cylinder("Branch",root,pos,scale,rot,mat,false);
        }

        static void DetailSchoolyard(Transform root, Material packed, Material dirtySnow, Material metal, Material wood, Material snow, Material warm) {
            DestroyByPrefix(root, "V12 yard");

            Bench(root,new Vector3(-18f,.18f,-8f),Quaternion.Euler(0f,18f,0f),metal,wood,"V12 yard bench A");
            Bench(root,new Vector3(11f,.12f,-7f),Quaternion.Euler(0f,-12f,0f),metal,wood,"V12 yard bench B");
            Bin(root,new Vector3(-15.4f,.15f,-7.2f),metal,"V12 yard bin A");
            BicycleRack(root,new Vector3(-4f,.12f,12.8f),metal,"V12 yard bike rack");
            NoticeBoard(root,new Vector3(-22f,.18f,-28f),metal,wood,"V12 yard notice board");

            for (int i=0;i<8;i++) {
                float x=-40f+i*6.1f;
                float y=.22f + (i>3 ? .16f : 0f);
                float z=-23f - i*.65f;
                GameObject drift=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drift.name="V12 yard transition snowbank";
                drift.transform.SetParent(root,false);
                drift.transform.localPosition=new Vector3(x,y,z);
                drift.transform.localScale=new Vector3(3.2f+(i%3)*.7f,.42f+(i%2)*.12f,.95f);
                drift.GetComponent<Renderer>().sharedMaterial=dirtySnow;
                UnityEngine.Object.DestroyImmediate(drift.GetComponent<Collider>());
            }

            Cube("V12 yard worn snow strip",root,new Vector3(-27.8f,.69f,-34f),new Vector3(4.2f,.025f,13f),packed,false);
            EntranceMarkerLight(root,new Vector3(-30f,.83f,-31.2f),metal,warm);
        }

        static void Bench(Transform parent, Vector3 pos, Quaternion rot, Material metal, Material wood, string name) {
            Transform b=new GameObject(name).transform; b.SetParent(parent,false); b.localPosition=pos; b.localRotation=rot;
            Cube("Seat",b,new Vector3(0f,.55f,0f),new Vector3(2.5f,.14f,.55f),wood,false);
            Cube("Back",b,new Vector3(0f,1.05f,.25f),new Vector3(2.5f,.65f,.10f),wood,false);
            Cube("Leg L",b,new Vector3(-.9f,.28f,0f),new Vector3(.12f,.55f,.45f),metal,false);
            Cube("Leg R",b,new Vector3(.9f,.28f,0f),new Vector3(.12f,.55f,.45f),metal,false);
        }

        static void Bin(Transform parent, Vector3 pos, Material metal, string name) {
            Transform b=new GameObject(name).transform; b.SetParent(parent,false); b.localPosition=pos;
            Cylinder("Bin body",b,new Vector3(0f,.5f,0f),new Vector3(.28f,.50f,.28f),Quaternion.identity,metal,false);
            Cylinder("Bin rim",b,new Vector3(0f,1.02f,0f),new Vector3(.31f,.055f,.31f),Quaternion.identity,metal,false);
        }

        static void BicycleRack(Transform parent, Vector3 pos, Material metal, string name) {
            Transform r=new GameObject(name).transform; r.SetParent(parent,false); r.localPosition=pos;
            Cube("Base",r,new Vector3(0f,.10f,0f),new Vector3(3.4f,.10f,.16f),metal,false);
            for(int i=0;i<6;i++) {
                float x=-1.4f+i*.56f;
                Cylinder("Rack upright",r,new Vector3(x,.52f,0f),new Vector3(.035f,.42f,.035f),Quaternion.identity,metal,false);
            }
        }

        static void NoticeBoard(Transform parent, Vector3 pos, Material metal, Material board, string name) {
            Transform n=new GameObject(name).transform; n.SetParent(parent,false); n.localPosition=pos;
            Cube("Board",n,new Vector3(0f,1.45f,0f),new Vector3(1.8f,1.05f,.10f),board,false);
            Cube("Post L",n,new Vector3(-.72f,.72f,0f),new Vector3(.10f,1.45f,.10f),metal,false);
            Cube("Post R",n,new Vector3(.72f,.72f,0f),new Vector3(.10f,1.45f,.10f),metal,false);
            Cube("Cap",n,new Vector3(0f,2.02f,0f),new Vector3(2.0f,.10f,.18f),metal,false);
        }

        static void EntranceMarkerLight(Transform parent, Vector3 pos, Material metal, Material warm) {
            Transform l=new GameObject("V12 yard bollard light").transform; l.SetParent(parent,false); l.localPosition=pos;
            Cylinder("Post",l,new Vector3(0f,.50f,0f),new Vector3(.075f,.50f,.075f),Quaternion.identity,metal,false);
            Cube("Glow",l,new Vector3(0f,.93f,0f),new Vector3(.22f,.18f,.22f),warm,false);
        }

        static void TuneLighting(Transform root) {
            Light sun = null;
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if (l.type == LightType.Directional) { sun=l; break; }
            }
            if (sun) {
                sun.intensity=.82f;
                sun.color=new Color(.86f,.91f,1f);
                sun.shadows=LightShadows.Soft;
                sun.shadowStrength=.72f;
            }
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.45f,.49f,.51f);
            RenderSettings.fog=true;
            RenderSettings.fogColor=new Color(.61f,.68f,.72f);
            RenderSettings.fogDensity=.0032f;
        }
    }
}
#endif
